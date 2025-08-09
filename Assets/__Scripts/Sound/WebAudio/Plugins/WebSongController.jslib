var SongController = {

    InitSongController: function (volume) {
        if (typeof SongCtx === 'undefined') {
            SongCtx = new AudioContext();
        }

        this.playing = false;
        this.volume = volume;
        this.playbackSpeed = 1;
        this.lastPlayed = SongCtx.currentTime;
        this.soundStartTime = 0;
        this.soundOffset = 0;

        this.lastContextTime = null;
        this.lastFrameTime = null;

        this.gainNode = SongCtx.createGain();
        this.gainNode.gain.setValueAtTime(0.0001, SongCtx.currentTime);
        this.gainNode.connect(SongCtx.destination);

        if (this.volume < 0.0001) {
            this.volume = 0.0001;
        }
    },

    DisposeSongClip: function () {
        if(!this.clip) {
            return;
        }

        if (this.playing) {
            this.clip.stop();
            this.clip.disconnect(this.gainNode);
        }

        delete (this.clip.buffer);
        delete (this.clip);

        this.playing = false;
    },

    UploadSongData: function (data, dataLength, isOgg, gameObjectName, methodName) {
        //Convert the C# byte[] to an arraybuffer for audio decoding
        const byteArray = new Uint8Array(dataLength);
        for (var i = 0; i < dataLength; i++) {
            byteArray[i] = HEAPU8[data + i];
        }

        gameObjectName = UTF8ToString(gameObjectName);
        methodName = UTF8ToString(methodName);

        let decodeFunction = (data, callback, errorCallback) => SongCtx.decodeAudioData(data, callback, errorCallback);
        if (isOgg && isSafari) {
            console.log("Using custom OggDecode module for Safari.");
            decodeFunction = (data, callback, errorCallback) => SongCtx.decodeOggData(data, callback, errorCallback);
        }

        if (this.clip) {
            if (this.playing) {
                this.clip.stop();
                this.clip.disconnect(this.gainNode);
            }

            delete (this.clip.buffer);
            delete (this.clip);

            this.playing = false;
        }

        decodeFunction(byteArray.buffer,
            (decodedData) => {
                const newClip = SongCtx.createBufferSource();
                newClip.buffer = decodedData;

                this.clip = newClip;

                //Callback to C# says that decoding succeeded
                SendMessage(gameObjectName, methodName, 1);
            },
            (err) => {
                console.error("Error decoding audio data: " + err.err);

                //Callback to C# says that decoding failed
                SendMessage(gameObjectName, methodName, 0);
            });
    },

    SetSongOffset: function (offset) {
        this.soundOffset = offset;
    },

    StartSong: function (time) {
        if (this.playing) {
            return;
        }

        this.gainNode.gain.setValueAtTime(0.0001, SongCtx.currentTime);
        if (this.volume > 0.0001) {
            this.gainNode.gain.exponentialRampToValueAtTime(this.volume, SongCtx.currentTime + 0.075);
        }

        //Create a new clip to play because after it plays it's forfeit
        const newClip = SongCtx.createBufferSource();

        newClip.buffer = this.clip.buffer;
        newClip.playbackRate.value = this.playbackSpeed;
        newClip.connect(this.gainNode);

        //Schedule the music to start playing 35ms in the future to avoid desync
        this.lastPlayed = SongCtx.currentTime + 0.035;
        this.soundStartTime = time;

        let startTime = time + this.soundOffset;
        if (startTime >= 0) {
            //Start the clip normally
            newClip.start(this.lastPlayed, startTime);
        }
        else {
            //Schedule the sound to be played ahead of time if playing at negative time
            if (this.playbackSpeed > 0) {
                //Account for playback speed, but don't divide by 0
                startTime /= this.playbackSpeed;
            }
            else startTime = 0;

            //Subtract startTime here because it's negative
            newClip.start(this.lastPlayed - startTime, 0);
        }

        this.lastContextTime = null;
        this.lastFrameTime = null;

        delete (this.clip);
        this.clip = newClip;
        this.playing = true;
    },

    StopSong: function () {
        if (!this.playing) {
            return;
        }

        this.gainNode.gain.setValueAtTime(this.volume, SongCtx.currentTime);
        this.gainNode.gain.exponentialRampToValueAtTime(0.0001, SongCtx.currentTime + 0.075);

        const clip = this.clip;
        const wasPlaying = this.playing;
        const oldGain = this.gainNode;
        const oldCtx = SongCtx;

        //Create a new audio context to avoid desync stemming from AudioContext.currentTime
        SongCtx = new AudioContext();
        this.gainNode = SongCtx.createGain();
        this.gainNode.gain.setValueAtTime(0.0001, SongCtx.currentTime);
        this.gainNode.connect(SongCtx.destination);

        this.playing = false;
        setTimeout(function () {
            if (clip && wasPlaying) {
                clip.stop();
                clip.disconnect(oldGain);
            }

            oldGain.disconnect(oldCtx.destination);

            this.lastContextTime = null;
            this.lastFrameTime = null;

            delete (oldGain);
            oldCtx.close();
            delete (oldCtx);
        }, 75);
    },

    GetSongTime: function () {
        //Use the high-precision performance.now() method to calculate a frame delta
        const frameTime = performance.now() / 1000;
        const delta = this.lastFrameTime != null ? Math.max(0, frameTime - this.lastFrameTime) : 0;

        let currentTime = SongCtx.currentTime;
        if(this.lastContextTime && currentTime - this.lastContextTime < 0.0001) {
            //Use frame delta if we have not proceeded a full AudioCtx timestep
            currentTime = this.lastContextTime + delta;
        }

        this.lastContextTime = currentTime;
        this.lastFrameTime = frameTime;

        const passedTime = currentTime - this.lastPlayed;
        return this.soundStartTime + (passedTime * this.playbackSpeed);
    },

    GetSongLength: function () {
        if (!this.clip) {
            return 0;
        }

        const buffer = this.clip.buffer;
        if (!buffer) {
            return 0;
        }

        return buffer.duration - this.soundOffset;
    },

    SetSongVolume: function (volume) {
        this.volume = volume;
        if (this.volume < 0.0001) {
            this.volume = 0.0001;
        }

        if (this.playing) {
            this.gainNode.gain.setValueAtTime(volume, SongCtx.currentTime);
        }
    },

    SetSongPlaybackSpeed: function (speed) {
        if (this.playing) {
            const passedTime = (SongCtx.currentTime - this.lastPlayed) + 0.035;
            let time = soundStartTime + (passedTime * this.playbackSpeed);

            this.lastPlayed = SongCtx.currentTime + 0.035;
            let startTime = time + this.soundOffset;

            if (startTime < 0) {
                //The sound is scheduled, but hasn't played yet. Reschedule, accounting for the new playback speed
                //This fixes a very niche bug where changing playback speed with negative offset,
                //before the sound actually starts playing, causes it to desync
                const clip = this.clip;
                clip.stop();
                clip.disconnect(this.gainNode);

                this.gainNode.disconnect(SongCtx.destination);
                delete (this.gainNode);
                SongCtx.close();
                delete (SongCtx);

                SongCtx = new AudioContext();
                this.gainNode = SongCtx.createGain();
                this.gainNode.gain.setValueAtTime(this.volume, SongCtx.currentTime);
                this.gainNode.connect(SongCtx.destination);

                const newClip = SongCtx.createBufferSource();
                newClip.buffer = clip.buffer;
                newClip.playbackRate.value = speed;

                startTime /= speed;

                //Subtract startTime here because it's negative
                newClip.start(this.lastPlayed - startTime, 0);
                newClip.connect(this.gainNode);

                delete (this.clip);
                this.clip = newClip;
            }
            else {
                this.clip.playbackRate.setValueAtTime(speed, this.lastPlayed);
            }

            this.soundStartTime = time;
        }

        this.playbackSpeed = speed;
    },

    GetSongPlaybackSpeed: function () {
        return this.playbackSpeed;
    }
};

mergeInto(LibraryManager.library, SongController);