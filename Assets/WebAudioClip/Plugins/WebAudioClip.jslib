mergeInto(LibraryManager.library, {

  Initcontroller: function(volume) {
    this.audioCtx = new AudioContext();
    this.clips = {};
    this.soundStartTimes = {};

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

  CreateClip: function(id, channels, length, frequency) {
    const clip = this.audioCtx.createBufferSource();
    const buffer = this.audioCtx.createBuffer(channels, length, frequency);

    clip.buffer = buffer;
    this.clips[id] = clip;
  },

  DisposeClip: function(id) {
    delete(this.clips[id]);
    delete(this.soundStartTimes[id]);
  },

  UploadData: function(id, data, dataLength, frequency, gameObjectName, methodName) {
    //Convert the C# byte[] to an arraybuffer for audio decoding
    const byteArray = new Uint8Array(dataLength);
    for(var i = 0; i < dataLength; i++) {
      byteArray[i] = HEAPU8[data + i];
    }

    gameObjectName = UTF8ToString(gameObjectName);
    methodName = UTF8ToString(methodName);

    this.audioCtx.sampleRate = frequency;
    this.audioCtx.decodeAudioData(byteArray.buffer,
      (decodedData) => {
        const newClip = this.audioCtx.createBufferSource();
        newClip.buffer = decodedData;
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

  Start: function(id, time) {
    if(this.playing) {
      return;
    }

    this.gainNode.gain.setValueAtTime(0.0001, this.audioCtx.currentTime);
    if(this.volume > 0.0001) {
      this.gainNode.gain.exponentialRampToValueAtTime(this.volume, this.audioCtx.currentTime + 0.1);
    }

    const clip = this.clips[id];

    //Create a new clip to play because apparently once it plays it's forfeit
    const newClip = this.audioCtx.createBufferSource();

    newClip.buffer = clip.buffer;
    newClip.playbackRate.value = this.playbackSpeed;

    newClip.start(0, time);
    newClip.connect(this.gainNode);

    this.clips[id] = newClip;
    this.soundStartTimes[id] = time;
    this.lastPlayed = this.audioCtx.currentTime;

    this.audioCtx.resume();
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
      clip.stop();
      clip.disconnect(this.gainNode);

      //Don't stop the context if we've started playing again
      //This is pretty common when scrubbing through the track
      if(!this.playing) {
        this.audioCtx.suspend();
      }
    }, 100);
  },

  GetSoundTime: function(id) {
    const passedTime = this.audioCtx.currentTime - this.lastPlayed;
    return this.soundStartTimes[id] + (passedTime * this.playbackSpeed);
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
      this.soundStartTimes[id] += passedTime * this.playbackSpeed;
    }
    this.playbackSpeed = speed;
    this.lastPlayed = this.audioCtx.currentTime;
  }
});