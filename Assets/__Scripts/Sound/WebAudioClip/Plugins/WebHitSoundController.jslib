mergeInto(LibraryManager.library, {

    InitHitSoundController: function (volume) {
        if (typeof AudioCtx === 'undefined') {
            AudioCtx = new AudioContext();
        }

        this.hitSounds = {};
        this.volume = volume;

        this.lowestOpenID = 0;
        this.highestID = -1;

        if (this.volume < 0.0001) {
            this.volume = 0.0001;
        }

        this.hitAudio = new Audio("TemplateData/SFX/Hitsounds/RabbitViewerTick.wav");
        this.badCutAudio = new Audio("TemplateData/SFX/BadHitsounds/BloopBadHitsound.wav");

        this.gainNode = AudioCtx.createGain();
        this.gainNode.gain.setValueAtTime(this.volume, AudioCtx.currentTime);
        this.gainNode.connect(AudioCtx.destination);
    },

    ScheduleHitSound: function (id) {
        //Schedules an already existing hitsound to be played
        //All hitsounds are offset by 0.185 seconds in their audio
        const universalDelay = 0.185;
        const hitSound = this.hitSounds[id];

        hitSound[audio].pause();
        
        //Calculate the schedule delay with respect to song playback speed
        const audioDelay = universalDelay / hitSound[speed];
        let delay = ((hitSound[startTime] - GetSongTime()) / GetSongPlaybackSpeed()) - audioDelay;

        if (delay >= 0) {
            //The audio can be played normally
            hitSound[audio].currentTime = 0;
            hitSound[audio].play(AudioContext.currentTime + delay);
        }
        else if (delay + audioDelay >= 0) {
            //The audio delay time has already passed, try playing the hitsound with no audio delay
            hitSound[audio].currentTime = universalDelay;
            hitSound[audio].play(AudioContext.currentTime + delay + audioDelay);
        }
    },

    RescheduleHitSounds: function () {
        //Loop over each hitsound and reschedule them
        for (let id = 0; id <= this.highestID; id++) {
            ScheduleHitSound(id);
        }
    },

    AddHitSound: function (badCut, playTime, pitch) {
        const newID = this.lowestOpenID;

        //Create the audio source and connect it to the destination
        const newAudio = badCut ? this.badCutAudio.cloneNode(true) : this.hitAudio.cloneNode(true);
        newAudio.playbackRate = pitch;
        newAudio.connect(this.gainNode);
        
        //Create an object to store relevant data for this hitsound
        this.hitSounds[newID] = {
            audio: newAudio,
            startTime: playTime,
            speed: pitch
        };

        //Schedule the hitsound to be played
        ScheduleHitSound(newID);

        //Update the lowest and highest IDs
        this.lowestOpenID++;
        while (this.hitSounds[this.lowestOpenID]) {
            this.lowestOpenID++;
        }
        if (newID > this.highestID) {
            this.highestID = newID;
        }
    },

    DisposeHitSound: function (id) {
        //Stop playing and delete the hitsound
        this.hitSounds[id][audio].pause();

        delete (this.hitSounds[id]);
        this.hitSounds[id] = null;

        //Update the lowest and highest IDs
        if (id < lowestOpenID) {
            lowestOpenID = id;
        }
        if (id >= highestID) {
            this.highestID--;
            while (!this.hitSounds[this.highestID]) {
                this.highestID--;
            }
        }
    }
});