mergeInto(LibraryManager.library, {

  Initcontroller: function(volume) {
    this.audioCtx = new AudioContext();
    this.clips = {};
    this.soundStartTimes = {};
    this.soundOffsets = {};
    this.clipPlaying = {};

    this.playing = false;
    this.volume = volume;
    this.playbackSpeed = 1;
    this.lastPlayed = this.audioCtx.currentTime;

    this.gainNode = this.audioCtx.createGain();
    this.gainNode.gain.setValueAtTime(0.0001, this.audioCtx.currentTime);
    this.gainNode.connect(this.audioCtx.destination);

    if(this.volume < 0.0001)
    {
      this.volume = 0.0001;
    }
  },

  CreateClip: function(id) {
    const clip = this.audioCtx.createBufferSource();

    this.clips[id] = clip;
    this.soundOffsets[id] = 0;
  },

  DisposeClip: function(id) {
    delete(this.clips[id]);
    delete(this.soundStartTimes[id]);
    delete(this.soundOffsets[id]);
  },

  UploadData: function(id, data, dataLength, isOgg, gameObjectName, methodName) {
    //Convert the C# byte[] to an arraybuffer for audio decoding
    const byteArray = new Uint8Array(dataLength);
    for(var i = 0; i < dataLength; i++) {
      byteArray[i] = HEAPU8[data + i];
    }

    gameObjectName = UTF8ToString(gameObjectName);
    methodName = UTF8ToString(methodName);

    let decodeFunction = (data, callback, errorCallback) => this.audioCtx.decodeAudioData(data, callback, errorCallback);
    if(isOgg && isSafari) {
      console.log("Using custom OggDecode module for Safari.");
      decodeFunction = (data, callback, errorCallback) => this.audioCtx.decodeOggData(data, callback, errorCallback);
    }

    decodeFunction(byteArray.buffer,
      (decodedData) => {
        const newClip = this.audioCtx.createBufferSource();
        newClip.buffer = decodedData;

        delete(this.clips[id]);
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

  SetOffset: function(id, offset) {
    this.soundOffsets[id] = offset;
  },

  Start: function(id, time) {
    if(this.playing) {
      return;
    }

    this.gainNode.gain.setValueAtTime(0.0001, this.audioCtx.currentTime);
    if(this.volume > 0.0001) {
      this.gainNode.gain.exponentialRampToValueAtTime(this.volume, this.audioCtx.currentTime + 0.1);
    }

    //Make sure the clip stops entirely
    const clip = this.clips[id];
    if(this.clipPlaying[id]) {
      clip.stop();
      clip.disconnect(this.gainNode);
      this.audioCtx.suspend();
    }

    //Create a new clip to play because apparently once it plays it's forfeit
    const newClip = this.audioCtx.createBufferSource();

    newClip.buffer = clip.buffer;
    newClip.playbackRate.value = this.playbackSpeed;

    this.audioCtx.resume();

    let startTime = time + this.soundOffsets[id];
    if(startTime >= 0) {
      //Start the clip normally
      newClip.start(0, startTime);
    }
    else {
      //Schedule the sound to be played ahead of time if playing at negative time
      if(this.playbackSpeed > 0) {
        //Account for playback speed, but don't divide by 0
        startTime /= this.playbackSpeed;
      }
      else startTime = 0;

      //Subtract startTime here because it's negative
      newClip.start(this.audioCtx.currentTime - startTime, 0);
    }
    newClip.connect(this.gainNode);

    delete(clip);
    this.clips[id] = newClip;
    this.clipPlaying[id] = true;

    this.soundStartTimes[id] = time;
    this.lastPlayed = this.audioCtx.currentTime;
    this.playing = true;
  },

  Stop: function(id) {
    if(!this.playing) {
      return;
    }

    this.gainNode.gain.setValueAtTime(this.volume, this.audioCtx.currentTime);
    this.gainNode.gain.exponentialRampToValueAtTime(0.0001, this.audioCtx.currentTime + 0.1);

    const clip = this.clips[id];
    this.playing = false;
    setTimeout(function() {
      //Don't stop the context if we've started playing again
      //This is pretty common when scrubbing through the track
      if(!this.playing) {
        this.audioCtx.suspend();

        if(this.clipPlaying[id]) {
          clip.stop();
          clip.disconnect(this.gainNode);
          this.clipPlaying[id] = false;
        }
      }
    }, 100);
  },

  GetSoundTime: function(id) {
    const passedTime = this.audioCtx.currentTime - this.lastPlayed;
    return this.soundStartTimes[id] + (passedTime * this.playbackSpeed);
  },

  GetSoundLength: function(id) {
    if(!this.clips[id]) {
      return 0;
    }

    const buffer = this.clips[id].buffer;
    if(!buffer) {
      return 0;
    }

    return buffer.duration - this.soundOffsets[id];
  },

  SetVolume: function(volume) {
    this.volume = volume;
    if(this.volume < 0.0001)
    {
      this.volume = 0.0001;
    }

    if(this.playing) {
      this.gainNode.gain.setValueAtTime(volume, this.audioCtx.currentTime);
    }
  },

  SetPlaybackSpeed: function(id, speed) {
    if(this.playing) {
      this.clips[id].playbackRate.value = speed;

      const passedTime = this.audioCtx.currentTime - this.lastPlayed;
      let time = soundStartTimes[id] + (passedTime * this.playbackSpeed);

      let startTime = time + this.soundOffsets[id];
      if(startTime < 0) {
        //The sound is scheduled, but hasn't played yet. Reschedule, accounting for the new playback speed
        //This fixes a very niche bug where changing playback speed with negative offset,
        //before the sound actually starts playing, causes it to desync
        const clip = this.clips[id];
        clip.stop();
        clip.disconnect(this.gainNode);

        const newClip = this.audioCtx.createBufferSource();
        newClip.buffer = clip.buffer;
        newClip.playbackRate.value = speed;

        if(speed > 0) {
          //Account for playback speed, but don't divide by 0
          startTime /= speed;
        }
        else startTime = 0;
  
        //Subtract startTime here because it's negative
        newClip.start(this.audioCtx.currentTime - startTime, 0);
        newClip.connect(this.gainNode);

        delete(this.clips[id]);
        this.clips[id] = newClip;
      }

      this.soundStartTimes[id] = time;
    }
    this.playbackSpeed = speed;
    this.lastPlayed = this.audioCtx.currentTime;
  }
});