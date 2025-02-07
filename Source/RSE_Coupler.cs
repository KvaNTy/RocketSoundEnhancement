﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RocketSoundEnhancement
{
    public class RSE_Coupler : RSE_Module
    {
        FXGroup fxGroup;
        bool isDecoupler;

        public override void OnStart(StartState state)
        {
            if(state == StartState.Editor || state == StartState.None)
                return;

            string partParentName = part.name + "_" + this.moduleName;
            audioParent = AudioUtility.CreateAudioParent(part, partParentName);

            var configNode = AudioUtility.GetConfigNode(part.partInfo.name, this.moduleName);

            SoundLayers = AudioUtility.CreateSoundLayerGroup(configNode.GetNodes("SOUNDLAYER"));

            if(part.isLaunchClamp()) {
                fxGroup = part.findFxGroup("activate");
                isDecoupler = true;
            }

            if(part.GetComponent<ModuleDecouplerBase>()) {
                fxGroup = part.findFxGroup("decouple");
                isDecoupler = true;
            }

            if(fxGroup != null) {
                if(SoundLayers.Where(x => x.name == fxGroup.name).Count() > 0) {
                    var soundLayer = SoundLayers.Find(x => x.name == fxGroup.name);
                    if(soundLayer.audioClips != null) {
                        var clip = GameDatabase.Instance.GetAudioClip(soundLayer.audioClips[0]);
                        if(clip != null) {
                            fxGroup.sfx = clip;
                            fxGroup.audio = AudioUtility.CreateOneShotSource(
                                audioParent,
                                soundLayer.volume * GameSettings.SHIP_VOLUME,
                                soundLayer.pitch,
                                soundLayer.spread);

                            Sources.Add(soundLayer.name, fxGroup.audio);
                        }
                    }
                }
            }

            GameEvents.onGameUnpause.Add(UpdateVolumes);
            GameEvents.onDockingComplete.Add(onDock);
            GameEvents.onPartUndockComplete.Add(onUnDock);
        }

        private void onUnDock(Part data)
        {
            if(part.flightID == data.flightID && !isDecoupler) {
                PlaySound("undock");
            }
        }

        private void onDock(GameEvents.FromToAction<Part, Part> data)
        {
            if(part.flightID == data.from.flightID && !isDecoupler) {
                PlaySound("dock");
            }
        }

        public void UpdateVolumes()
        {
            foreach(var sound in SoundLayers) {
                if(Sources.ContainsKey(sound.name)) {
                    Sources[sound.name].volume = sound.volume * GameSettings.SHIP_VOLUME;
                }
            }
        }

        public override void OnUpdate()
        {
            if(!HighLogic.LoadedSceneIsFlight)
                return;

            foreach(var asource in Sources.Keys) {
                if(asource == "decouple" || asource == "activate")
                    continue;

                if(!Sources[asource].isPlaying) {
                    UnityEngine.Object.Destroy(Sources[asource]);
                    Sources.Remove(asource);
                }
            }
        }

        public void PlaySound(string action)
        {
            if(SoundLayers.Where(x => x.name == action).Count() > 0) {
                var soundLayer = SoundLayers.Find(x => x.name == action);

                if(soundLayer.audioClips == null)
                    return;

                AudioSource source;
                if(Sources.ContainsKey(action)) {
                    source = Sources[action];
                } else {
                    source = AudioUtility.CreateOneShotSource(
                        audioParent,
                        soundLayer.volume * GameSettings.SHIP_VOLUME,
                        soundLayer.pitch,
                        soundLayer.spread);
                    Sources.Add(soundLayer.name, source);
                }

                var clip = GameDatabase.Instance.GetAudioClip(soundLayer.audioClips[0]);
                if(clip != null) {
                    source.PlayOneShot(clip);
                }
            }
        }

        public new void OnDestroy()
        {
            foreach(var source in Sources.Keys) {
                GameObject.Destroy(Sources[source]);
            }

            GameEvents.onGameUnpause.Remove(UpdateVolumes);
            GameEvents.onDockingComplete.Remove(onDock);
            GameEvents.onPartUndockComplete.Remove(onUnDock);
        }
    }
}
