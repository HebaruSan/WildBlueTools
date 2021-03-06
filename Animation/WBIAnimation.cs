﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIAnimation : ExtendedPartModule
    {
        protected const int kDefaultAnimationLayer = 2;

        [KSPField()]
        public int animationLayer = kDefaultAnimationLayer;

        [KSPField()]
        public string animationName;

        [KSPField()]
        public string startEventGUIName;

        [KSPField()]
        public string endEventGUIName;

        [KSPField(isPersistant = true)]
        public bool guiIsVisible = true;

        [KSPField]
        public string startSoundURL = string.Empty;

        [KSPField]
        public float startSoundPitch = 1.0f;

        [KSPField]
        public float startSoundVolume = 0.5f;

        [KSPField]
        public string loopSoundURL = string.Empty;

        [KSPField]
        public float loopSoundPitch = 1.0f;

        [KSPField]
        public float loopSoundVolume = 0.5f;

        [KSPField]
        public string stopSoundURL = string.Empty;

        [KSPField]
        public float stopSoundPitch = 1.0f;

        [KSPField]
        public float stopSoundVolume = 0.5f;

        //Helper objects
        public bool isDeployed = false;
        public bool isMoving = false;
        public Animation animation = null;
        protected AnimationState animationState;
        protected AudioSource loopSound = null;
        protected AudioSource startSound = null;
        protected AudioSource stopSound = null;

        #region User Events & API
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ToggleAnimation", active = true, externalToEVAOnly = false, unfocusedRange = 3.0f, guiActiveUnfocused = true)]
        public virtual void ToggleAnimation()
        {
            //Play animation for current state
            PlayAnimation(isDeployed);

            //Toggle state
            isDeployed = !isDeployed;
            if (isDeployed)
            {
                Events["ToggleAnimation"].guiName = endEventGUIName;
            }
            else
            {
                Events["ToggleAnimation"].guiName = startEventGUIName;
            }

            Log("Animation toggled new gui name: " + Events["ToggleAnimation"].guiName);
        }

        [KSPAction("ToggleAnimation")]
        public virtual void ToggleAnimationAction(KSPActionParam param)
        {
            ToggleAnimation();
        }

        public virtual void ToggleAnimation(bool deployed)
        {
            isDeployed = deployed;

            //Play animation for current state
            PlayAnimation(isDeployed);

            if (isDeployed)
                Events["ToggleAnimation"].guiName = endEventGUIName;
            else
                Events["ToggleAnimation"].guiName = startEventGUIName;
        }

        public virtual void showGui(bool isVisible)
        {
            guiIsVisible = isVisible;
            Events["ToggleAnimation"].guiActive = isVisible;
            Events["ToggleAnimation"].guiActiveEditor = isVisible;
            Events["ToggleAnimation"].guiActiveUnfocused = isVisible;
        }

        #endregion

        #region Overrides
        public override void OnUpdate()
        {
            base.OnUpdate();

            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (animation == null)
                return;

            //Play start
            /*
            if (animation.isPlaying && isMoving == false)
            {
                isMoving = true;
                playStart();
            }
             */

            //Play end
            else if (animation.isPlaying == false && isMoving)
            {
                isMoving = false;
                playEnd();
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            string value;
            base.OnLoad(node);

            value = node.GetValue("isDeployed");
            if (string.IsNullOrEmpty(value) == false)
                isDeployed = bool.Parse(value);

            try
            {
                SetupAnimations();
            }

            catch (Exception ex)
            {
                Log("Error encountered while attempting to setup animations: " + ex.ToString());
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.AddValue("isDeployed", isDeployed.ToString());
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            SetupAnimations();
            setupSounds();
        }

        protected override void getProtoNodeValues(ConfigNode protoNode)
        {
            base.getProtoNodeValues(protoNode);

            animationName = protoNode.GetValue("animationName");

            endEventGUIName = protoNode.GetValue("endEventGUIName");

            startEventGUIName = protoNode.GetValue("startEventGUIName");
        }

        public void playStart()
        {
            if (startSound != null)
                startSound.Play();

            if (loopSound != null)
                loopSound.Play();
        }

        public void playEnd()
        {
            if (stopSound != null)
                stopSound.Play();

            if (loopSound != null)
                loopSound.Stop();
        }

        #endregion

        #region Helpers
        protected virtual void setupSounds()
        {
            if (!string.IsNullOrEmpty(startSoundURL))
            {
                startSound = gameObject.AddComponent<AudioSource>();
                startSound.clip = GameDatabase.Instance.GetAudioClip(startSoundURL);
                startSound.pitch = startSoundPitch;
                startSound.volume = GameSettings.SHIP_VOLUME * startSoundVolume;
            }

            if (!string.IsNullOrEmpty(loopSoundURL))
            {
                loopSound = gameObject.AddComponent<AudioSource>();
                loopSound.clip = GameDatabase.Instance.GetAudioClip(loopSoundURL);
                loopSound.loop = true;
                loopSound.pitch = loopSoundPitch;
                loopSound.volume = GameSettings.SHIP_VOLUME * loopSoundVolume;
            }

            if (!string.IsNullOrEmpty(stopSoundURL))
            {
                stopSound = gameObject.AddComponent<AudioSource>();
                stopSound.clip = GameDatabase.Instance.GetAudioClip(stopSoundURL);
                stopSound.pitch = stopSoundPitch;
                stopSound.volume = GameSettings.SHIP_VOLUME * stopSoundVolume;
            }
        }

        public virtual void SetupAnimations()
        {
            Log("SetupAnimations called.");

            Animation[] animations = this.part.FindModelAnimators(animationName);
            if (animations == null)
            {
                Log("No animations found.");
                return;
            }
            if (animations.Length == 0)
            {
                Log("No animations found.");
                return;
            }

            animation = animations[0];
            if (animation == null)
                return;

            //Set layer
            animationState = animation[animationName];
            animation[animationName].layer = animationLayer;

            //Set toggle button
            Events["ToggleAnimation"].guiActive = guiIsVisible;
            Events["ToggleAnimation"].guiActiveEditor = guiIsVisible;

            if (isDeployed)
            {
                Events["ToggleAnimation"].guiName = endEventGUIName;

                animation[animationName].normalizedTime = 1.0f;
                animation[animationName].speed = 10000f;
            }
            else
            {
                Events["ToggleAnimation"].guiName = startEventGUIName;

                animation[animationName].normalizedTime = 0f;
                animation[animationName].speed = -10000f;
            }
            animation.Play(animationName);
        }

        public virtual void PlayAnimation(bool playInReverse = false)
        {
            if (string.IsNullOrEmpty(animationName))
                return;

            float animationSpeed = playInReverse == false ? 1.0f : -1.0f;
            Animation anim = this.part.FindModelAnimators(animationName)[0];

            if (playInReverse)
            {
                anim[animationName].time = anim[animationName].length;
                if (HighLogic.LoadedSceneIsFlight)
                    anim[animationName].speed = animationSpeed;
                else
                    anim[animationName].speed = animationSpeed * 100;
                anim.Play(animationName);
            }

            else
            {
                if (HighLogic.LoadedSceneIsFlight)
                    anim[animationName].speed = animationSpeed;
                else
                    anim[animationName].speed = animationSpeed * 100;
                anim.Play(animationName);
            }

            isMoving = true;
            playStart();
        }

        #endregion
    }
}
