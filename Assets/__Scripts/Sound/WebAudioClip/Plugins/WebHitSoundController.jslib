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
        fetch("TemplateData/SFX/Hitsounds/RabbitViewerTick.wav")
            .then((res) => res.arrayBuffer())
            .then((buffer) => AudioCtx.decodeAudioData(buffer))
            .then((buffer) => {
                this.badCutAudio = buffer;
            })

        this.gainNode = AudioCtx.createGain();
        this.gainNode.gain.setValueAtTime(this.volume, AudioCtx.currentTime);
        this.gainNode.connect(AudioCtx.destination);
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
        }
        else if (delay + audioDelay >= 0) {
            //The audio delay time has already passed, try playing the hitsound with no audio delay
            hitSound.node.start(AudioCtx.currentTime + delay + audioDelay, universalDelay);
        }

        hitSound.playing = true;

        //Automatically dispose the hitsound after it ends with a C# callback
        hitSound.node.addEventListener("ended", (e) => SendMessage("Web Hit Sound Controller", "DeleteHitSound", id));
    },

    DisposeHitSound: function (id) {
        if (typeof this.hitSounds[id] === 'undefined' || this.hitSounds[id] === null) {
            return;
        }

        //Stop playing and delete the hitsound
        if (typeof this.hitSounds[id].node !== 'undefined' && this.hitSounds[id].node !== null) {
            this.hitSounds[id].node.stop();
            this.hitSounds[id].node.disconnect(this.gainNode);
            delete (this.hitSounds[id].node)
        }

        delete (this.hitSounds[id]);
        this.hitSounds[id] = null;
    },

    AddHitSound: function (id, badCut, playTime, pitch) {
        //Create the audio node and connect it to the destination
        const newNode = AudioCtx.createBufferSource();
        newNode.buffer = badCut ? this.badCutAudio : this.hitAudio;
        newNode.playbackRate.value = pitch;
        newNode.connect(this.gainNode);
        
        //Create an object to store relevant data for this hitsound
        this.hitSounds[id] = {
            node: newNode,
            startTime: playTime,
            speed: pitch,
            playing: false
        };
    },

    GetHitSoundTime: function (id) {
        if (typeof this.hitSounds[id] === 'undefined') {
            return -1;
        }
        return this.hitSounds[id].startTime;
    }
};

mergeInto(LibraryManager.library, HitSoundController);