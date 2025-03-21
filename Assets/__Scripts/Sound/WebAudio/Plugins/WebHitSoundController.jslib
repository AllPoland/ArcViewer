var HitSoundController = {

    InitHitSoundController: function () {
        if (typeof AudioCtx === 'undefined') {
            AudioCtx = new AudioContext();
        }

        this.hitSounds = {};

        const hitSoundNames = [
            "RabbitViewerTick",
            "ChromapperTick",
            "OsuHitsound",
            "ThumpyHitsound",
            "GalxHitsound"
        ];

        const badHitSoundNames = [
            "BloopBadHitsound",
            "RecordScratchBadHitsound",
            "FunkyBadHitsound",
            "VineBoomBadHitsound"
        ];

        if (typeof this.hitSoundIndex === 'undefined') {
            this.hitSoundIndex = 0;
        }
        if (typeof this.badHitSoundIndex === 'undefined') {
            this.badHitSoundIndex = 0;
        }

        this.hitSoundBuffers = [];
        this.badHitSoundBuffers = [];

        //Load the hitsound audio
        for (let i = 0; i < hitSoundNames.length; i++) {
            fetch("TemplateData/SFX/Hitsounds/" + hitSoundNames[i] + ".wav")
                .then((res) => res.arrayBuffer())
                .then((buffer) => AudioCtx.decodeAudioData(buffer))
                .then((buffer) => {
                    this.hitSoundBuffers[i] = buffer;
                    if (i == this.hitSoundIndex) {
                        this.hitAudio = this.hitSoundBuffers[i];
                    }
                })
        }
        for (let i = 0; i < badHitSoundNames.length; i++) {
            fetch("TemplateData/SFX/BadHitsounds/" + badHitSoundNames[i] + ".wav")
                .then((res) => res.arrayBuffer())
                .then((buffer) => AudioCtx.decodeAudioData(buffer))
                .then((buffer) => {
                    this.badHitSoundBuffers[i] = buffer;
                    if (i == this.badHitSoundIndex) {
                        this.badHitAudio = this.badHitSoundBuffers[i];
                    }
                })
        }

        if (typeof this.hitSoundGain === 'undefined') {
            this.hitSoundGain = AudioCtx.createGain();
            this.hitSoundGain.gain.setValueAtTime(1.0, AudioCtx.currentTime);
            this.hitSoundGain.connect(AudioCtx.destination);
        }

        if (typeof this.chainSoundGain === 'undefined') {
            this.chainSoundGain = AudioCtx.createGain();
            this.chainSoundGain.gain.setValueAtTime(0.8, AudioCtx.currentTime);
            this.chainSoundGain.connect(this.hitSoundGain);
        }
    },

    SetHitSoundVolume: function (volume) {
        if (typeof AudioCtx === 'undefined') {
            AudioCtx = new AudioContext();
        }
        if (typeof this.hitSoundGain === 'undefined') {
            this.hitSoundGain = AudioCtx.createGain();
            this.hitSoundGain.connect(AudioCtx.destination);
        }

        this.hitSoundGain.gain.setValueAtTime(volume, AudioCtx.currentTime);
    },

    SetChainSoundVolume: function (volume) {
        if (typeof AudioCtx === 'undefined') {
            AudioCtx = new AudioContext();
        }
        if (typeof this.hitSoundGain === 'undefined') {
            this.hitSoundGain = AudioCtx.createGain();
            this.hitSoundGain.gain.setValueAtTime(1.0, AudioCtx.currentTime);
            this.hitSoundGain.connect(AudioCtx.destination);
        }
        if (typeof this.chainSoundGain === 'undefined') {
            this.chainSoundGain = AudioCtx.createGain();
            this.chainSoundGain.connect(this.hitSoundGain);
        }

        this.chainSoundGain.gain.setValueAtTime(volume, AudioCtx.currentTime);
    },

    SetHitSound: function (hitSound) {
        this.hitSoundIndex = hitSound;
        if (typeof this.hitSoundBuffers !== 'undefined' && this.hitSoundBuffers.length > hitSound) {
            //Only update the buffer if the hitsounds are initialized
            //otherwise the hitsound will automatically be picked on init
            this.hitAudio = this.hitSoundBuffers[hitSound];
        }
    },

    SetBadHitSound: function (badHitSound) {
        this.badHitSoundIndex = badHitSound;
        if (typeof this.badHitSoundBuffers !== 'undefined' && this.badHitSoundBuffers.length > badHitSound) {
            //Only update the buffer if the hitsounds are initialized
            //otherwise the hitsound will automatically be picked on init
            this.badHitAudio = this.badHitSoundBuffers[badHitSound];
        }
    },

    ScheduleHitSound: function (id, songTime, songPlaybackSpeed) {
        //Schedules an already existing hitsound to be played
        //All hitsounds are offset by 0.185 seconds in their audio
        const universalDelay = 0.185;
        const hitSound = this.hitSounds[id];

        if (typeof hitSound === 'undefined') {
            return;
        }

        if (hitSound.playing) {
            return;
        }
        
        //Calculate the schedule delay with respect to song playback speed
        const audioDelay = universalDelay / hitSound.speed;
        let delay = ((hitSound.startTime - songTime) / songPlaybackSpeed) - audioDelay;

        if (delay >= 0) {
            //The audio can be played normally
            hitSound.node.start(AudioCtx.currentTime + delay);
            hitSound.playing = true;
        }
        else if (delay + audioDelay >= 0) {
            //The audio delay time has already passed, try playing the hitsound with no audio delay
            hitSound.node.start(AudioCtx.currentTime + delay + audioDelay, universalDelay);
            hitSound.playing = true;
        }
        else {
            //The hitsound has already passed, just dispose of it
            SendMessage("Web Hit Sound Controller", "DeleteHitSound", id);
            return;
        }

        //Automatically dispose the hitsound after it ends with a C# callback
        hitSound.callback = (e) => SendMessage("Web Hit Sound Controller", "DeleteHitSound", id);
        hitSound.node.addEventListener("ended", hitSound.callback);
    },

    RemakeHitSound: function (id) {
        if (typeof this.hitSounds[id] === 'undefined' || this.hitSounds[id] === null) {
            return;
        }

        const hitSound = this.hitSounds[id];

        if (typeof hitSound.node !== 'undefined' && this.hitSounds[id].node !== null) {
            //Stop playing the sound if it is playing
            if (hitSound.playing) {
                hitSound.node.removeEventListener("ended", hitSound.callback);
                hitSound.node.stop();
            }

            if (!hitSound.isChainLink) {
                hitSound.node.disconnect(this.hitSoundGain);
            }
            else hitSound.node.disconnect(this.chainSoundGain);
            delete (hitSound.node)
        }

        //Create a new audio source for this hitsound
        const newNode = AudioCtx.createBufferSource();
        newNode.buffer = hitSound.isBadCut ? this.badHitAudio : this.hitAudio;
        newNode.playbackRate.value = hitSound.speed;
        if (!hitSound.isChainLink) {
            newNode.connect(this.hitSoundGain);
        }
        else newNode.connect(this.chainSoundGain);

        hitSound.node = newNode;
        hitSound.playing = false;
    },

    DisposeHitSound: function (id) {
        if (typeof this.hitSounds[id] === 'undefined' || this.hitSounds[id] === null) {
            return;
        }

        //Stop playing and delete the hitsound
        const hitSound = this.hitSounds[id];
        if (typeof hitSound.node !== 'undefined' && this.hitSounds[id].node !== null) {
            if (hitSound.playing) {
                hitSound.node.removeEventListener("ended", hitSound.callback);
                hitSound.node.stop();
            }

            if (!hitSound.isChainLink) {
                hitSound.node.disconnect(this.hitSoundGain);
            }
            else hitSound.node.disconnect(this.chainSoundGain);
            delete (hitSound.node)
            delete (this.hitSounds[id]);
        }

        delete (hitSound);
        this.hitSounds[id] = null;
    },

    AddHitSound: function (id, badCut, chainLink, playTime, pitch) {
        //Create the audio node and connect it to the destination
        const newNode = AudioCtx.createBufferSource();
        newNode.buffer = badCut ? this.badHitAudio : this.hitAudio;
        newNode.playbackRate.value = pitch; 

        if (!chainLink) {
            newNode.connect(this.hitSoundGain);
        }
        else newNode.connect(this.chainSoundGain);

        //Create an object to store relevant data for this hitsound
        this.hitSounds[id] = {
            node: newNode,
            startTime: playTime,
            speed: pitch,
            isBadCut: badCut,
            isChainLink: chainLink,
            playing: false
        };
    },

    GetHitSoundTime: function (id) {
        if (typeof this.hitSounds[id] === 'undefined' || this.hitSounds[id] === null) {
            return -1;
        }
        return this.hitSounds[id].startTime;
    },

    IsHitSoundBadCut: function (id) {
        if (typeof this.hitSounds[id] === 'undefined' || this.hitSounds[id] === null) {
            return false;
        }
        return this.hitSounds[id].isBadCut;
    }
};

mergeInto(LibraryManager.library, HitSoundController);