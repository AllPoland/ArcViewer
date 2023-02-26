mergeInto(LibraryManager.library, {

  Initcontroller: function() {
    this.audioCtx = new AudioContext();
    this.clips = {};
    this.soundStartTimes = {};

    this.playing = false;
    this.volume = 0.5;
    this.playbackSpeed = 1;
    this.lastPlayed = this.audioCtx.currentTime;

    this.gainNode = this.audioCtx.createGain();
    this.gainNode.gain.setValueAtTime(0.001, this.audioCtx.currentTime);
    this.gainNode.connect(this.audioCtx.destination);
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

  UploadData: function(id, samples, size, offset, channels, frequency) {
    this.audioCtx.sampleRate = frequency;

    const clip = this.clips[id];
    if(!clip.buffer) {
      var buffer = this.audioCtx.createBuffer(channels, size, frequency);
    }
    else {
      var buffer = clip.buffer;
    }

    for(var channel = 0; channel < channels; channel++) {
      const channelSamples = [];

      for(var i = channel; i < size; i += channels) {
        channelSamples.push(
          HEAPF32[(samples >> 2) + i]
        );
      }

      var bufferData = Float32Array.from(channelSamples);
      buffer.copyToChannel(bufferData, channel, offset);
    }

    //Create a new clip because you can't set the buffer of an old one
    const newClip = this.audioCtx.createBufferSource();
    newClip.buffer = buffer;
    this.clips[id] = newClip;
  },

  Start: function(id, time) {
    if(this.playing) {
      return;
    }

    this.gainNode.gain.setValueAtTime(0.0001, this.audioCtx.currentTime);
    this.gainNode.gain.exponentialRampToValueAtTime(this.volume, this.audioCtx.currentTime + 0.1);

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