mergeInto(LibraryManager.library, {

    InitSongController: function (volume) {
        AudioCtx = new AudioContext();
        this.clips = {};
        this.soundStartTimes = {};
        this.soundOffsets = {};
        this.clipPlaying = {};

        this.playing = false;
        this.volume = volume;
        this.playbackSpeed = 1;
        this.lastPlayed = AudioCtx.currentTime;

        this.gainNode = AudioCtx.createGain();
        this.gainNode.gain.setValueAtTime(0.0001, AudioCtx.currentTime);
        this.gainNode.connect(AudioCtx.destination);

        if (this.volume < 0.0001) {
            this.volume = 0.0001;
        }
    },

    CreateSongClip: function (id) {
        const clip = AudioCtx.createBufferSource();

        this.clips[id] = clip;
        this.soundOffsets[id] = 0;
    },

    DisposeSongClip: function (id) {
        delete (this.clips[id]);
        delete (this.soundStartTimes[id]);
        delete (this.soundOffsets[id]);
    },

    UploadSongData: function (id, data, dataLength, isOgg, gameObjectName, methodName) {
        //Convert the C# byte[] to an arraybuffer for audio decoding
        const byteArray = new Uint8Array(dataLength);
        for (var i = 0; i < dataLength; i++) {
            byteArray[i] = HEAPU8[data + i];
        }

        gameObjectName = UTF8ToString(gameObjectName);
        methodName = UTF8ToString(methodName);

        let decodeFunction = (data, callback, errorCallback) => AudioCtx.decodeAudioData(data, callback, errorCallback);
        if (isOgg && isSafari) {
            console.log("Using custom OggDecode module for Safari.");
            decodeFunction = (data, callback, errorCallback) => AudioCtx.decodeOggData(data, callback, errorCallback);
        }

        decodeFunction(byteArray.buffer,
            (decodedData) => {
                const newClip = AudioCtx.createBufferSource();
                newClip.buffer = decodedData;

                delete (this.clips[id]);
                this.clips[id] = newClip;

                //Callback to C# says that decoding succeeded
                SendMessage(gameObjectName, methodName, 1);
            },
            (err) => {
                console.error("Error decoding audio data: " + err.err);

                //Callback to C# says that decoding failed
                SendMessage(gameObjectName, methodName, 0);
            });
    },

    SetSongOffset: function (id, offset) {
        this.soundOffsets[id] = offset;
    },

    StartSong: function (id, time) {
        if (this.playing) {
            return;
        }

        this.gainNode.gain.setValueAtTime(0.0001, AudioCtx.currentTime);
        if (this.volume > 0.0001) {
            this.gainNode.gain.exponentialRampToValueAtTime(this.volume, AudioCtx.currentTime + 0.1);
        }

        //Make sure the clip stops entirely
        const clip = this.clips[id];
        if (this.clipPlaying[id]) {
            clip.stop();
            clip.disconnect(this.gainNode);
            AudioCtx.suspend();
        }

        //Create a new clip to play because apparently once it plays it's forfeit
        const newClip = AudioCtx.createBufferSource();

        newClip.buffer = clip.buffer;
        newClip.playbackRate.value = this.playbackSpeed;

        AudioCtx.resume();

        let startTime = time + this.soundOffsets[id];
        if (startTime >= 0) {
            //Start the clip normally
            newClip.start(0, startTime);
        }
        else {
            //Schedule the sound to be played ahead of time if playing at negative time
            if (this.playbackSpeed > 0) {
                //Account for playback speed, but don't divide by 0
                startTime /= this.playbackSpeed;
            }
            else startTime = 0;

            //Subtract startTime here because it's negative
            newClip.start(AudioCtx.currentTime - startTime, 0);
        }
        newClip.connect(this.gainNode);

        delete (clip);
        this.clips[id] = newClip;
        this.clipPlaying[id] = true;

        this.soundStartTimes[id] = time;
        this.lastPlayed = AudioCtx.currentTime;
        this.playing = true;
    },

    StopSong: function (id) {
        if (!this.playing) {
            return;
        }

        this.gainNode.gain.setValueAtTime(this.volume, AudioCtx.currentTime);
        this.gainNode.gain.exponentialRampToValueAtTime(0.0001, AudioCtx.currentTime + 0.1);

        const clip = this.clips[id];
        this.playing = false;
        setTimeout(function () {
            //Don't stop the context if we've started playing again
            //This is pretty common when scrubbing through the track
            if (!this.playing) {
                AudioCtx.suspend();

                if (this.clipPlaying[id]) {
                    clip.stop();
                    clip.disconnect(this.gainNode);
                    this.clipPlaying[id] = false;
                }
            }
        }, 100);
    },

    GetSongTime: function (id) {
        const passedTime = AudioCtx.currentTime - this.lastPlayed;
        return this.soundStartTimes[id] + (passedTime * this.playbackSpeed);
    },

    GetSongLength: function (id) {
        if (!this.clips[id]) {
            return 0;
        }

        const buffer = this.clips[id].buffer;
        if (!buffer) {
            return 0;
        }

        return buffer.duration - this.soundOffsets[id];
    },

    SetSongVolume: function (volume) {
        this.volume = volume;
        if (this.volume < 0.0001) {
            this.volume = 0.0001;
        }

        if (this.playing) {
            this.gainNode.gain.setValueAtTime(volume, AudioCtx.currentTime);
        }
    },

    SetSongPlaybackSpeed: function (id, speed) {
        if (this.playing) {
            this.clips[id].playbackRate.value = speed;

            const passedTime = AudioCtx.currentTime - this.lastPlayed;
            let time = soundStartTimes[id] + (passedTime * this.playbackSpeed);

            let startTime = time + this.soundOffsets[id];
            if (startTime < 0) {
                //The sound is scheduled, but hasn't played yet. Reschedule, accounting for the new playback speed
                //This fixes a very niche bug where changing playback speed with negative offset,
                //before the sound actually starts playing, causes it to desync
                const clip = this.clips[id];
                clip.stop();
                clip.disconnect(this.gainNode);

                const newClip = AudioCtx.createBufferSource();
                newClip.buffer = clip.buffer;
                newClip.playbackRate.value = speed;

                if (speed > 0) {
                    //Account for playback speed, but don't divide by 0
                    startTime /= speed;
                }
                else startTime = 0;

                //Subtract startTime here because it's negative
                newClip.start(AudioCtx.currentTime - startTime, 0);
                newClip.connect(this.gainNode);

                delete (this.clips[id]);
                this.clips[id] = newClip;
            }

            this.soundStartTimes[id] = time;
        }
        this.playbackSpeed = speed;
        this.lastPlayed = AudioCtx.currentTime;
    }
});