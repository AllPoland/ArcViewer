var HitSoundController = {

    InitHitSoundController: function (volume) {
        if (typeof AudioCtx === 'undefined') {
            AudioCtx = new AudioContext();
        }

        this.hitSounds = {};
        this.volume = volume;

        if (this.volume < 0.0001) {
            this.volume = 0.0001;
        }

        //Load the hitsound audio and create the base node
        fetch("TemplateData/SFX/Hitsounds/RabbitViewerTick.wav")
            .then((res) => res.arrayBuffer())
            .then((buffer) => AudioCtx.decodeAudioData(buffer))
            .then((buffer) => {
                this.hitAudio = buffer;
            })
        fetch("TemplateData/SFX/BadHitsounds/BloopBadHitsound.wav")
            .then((res) => res.arrayBuffer())
            .then((buffer) => AudioCtx.decodeAudioData(buffer))
            .then((buffer) => {
                this.badCutAudio = buffer;
            })

        this.hitSoundGain = AudioCtx.createGain();
        this.hitSoundGain.gain.setValueAtTime(this.volume, AudioCtx.currentTime);
        this.hitSoundGain.connect(AudioCtx.destination);
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
        }

        //Automatically dispose the hitsound after it ends with a C# callback
        hitSound.node.addEventListener("ended", (e) => SendMessage("Web Hit Sound Controller", "DeleteHitSound", id));
    },

    RemakeHitSound: function (id) {
        if (typeof this.hitSounds[id] === 'undefined' || this.hitSounds[id] === null) {
            return;
        }

        const hitSound = this.hitSounds[id];

        if (typeof hitSound.node !== 'undefined' && this.hitSounds[id].node !== null) {
            //Stop playing the sound if it is playing
            if (hitSound.playing) {
                hitSound.node.stop();
            }
            hitSound.node.disconnect(this.hitSoundGain);
            delete (hitSound.node)
        }

        //Create a new audio source for this hitsound
        const newNode = AudioCtx.createBufferSource();
        newNode.buffer = hitSound.isBadCut ? this.badCutAudio : this.hitAudio;
        newNode.playbackRate.value = hitSound.speed;
        newNode.connect(this.hitSoundGain);

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
                hitSound.node.stop();
            }
            hitSound.node.disconnect(this.hitSoundGain);
            delete (hitSound.node)
        }

        delete (hitSound);
        this.hitSounds[id] = null;
    },

    AddHitSound: function (id, badCut, playTime, pitch) {
        //Create the audio node and connect it to the destination
        const newNode = AudioCtx.createBufferSource();
        newNode.buffer = badCut ? this.badCutAudio : this.hitAudio;
        newNode.playbackRate.value = pitch; 
        newNode.connect(this.hitSoundGain);
        
        //Create an object to store relevant data for this hitsound
        this.hitSounds[id] = {
            node: newNode,
            startTime: playTime,
            speed: pitch,
            isBadCut: badCut,
            playing: false
        };
    },

    GetHitSoundTime: function (id) {
        if (typeof this.hitSounds[id] === 'undefined') {
            return -1;
        }
        return this.hitSounds[id].startTime;
    },

    IsHitSoundBadCut: function (id) {
        if (typeof this.hitSounds[id] === 'undefined') {
            return false;
        }
        return this.hitSounds[id].isBadCut;
    }
};

mergeInto(LibraryManager.library, HitSoundController);