using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lights
{
    public class PoliceLightsCarSystem : MonoBehaviour
    {
        public bool activeLight = true;

        public float time = 20;

        public AudioSource policeAudioSource;

        public AudioClip[] policeAudioClips;

        public Light[] RedLights;
        public Light[] BlueLights;

        private float timer = 0.0f;
        private int lightNum = 0;

        private void Awake()
        {
            if (policeAudioClips.Length > 0)
            {
                policeAudioSource.clip = policeAudioClips[Random.Range(0, policeAudioClips.Length)];

                policeAudioSource.Play();
            }
        }

        // Update is called once per frame
        private void Update()
        {
            if (!activeLight)
            {
                if (policeAudioSource.mute == false)
                {
                    policeAudioSource.clip = policeAudioClips[Random.Range(0, policeAudioClips.Length)];
                    policeAudioSource.mute = true;
                }

                foreach (Light RedLight in RedLights)
                {
                    RedLight.enabled = false;
                }

                foreach (Light BlueLight in BlueLights)
                {
                    BlueLight.enabled = false;
                }

                return;
            }

            timer = Mathf.MoveTowards(timer, 0.0f, Time.deltaTime * time);

            if (timer == 0)
            {
                lightNum++;

                if (lightNum > 12)
                {
                    lightNum = 1;
                }

                timer = 1.0f;
            }

            if (policeAudioSource)
            {
                policeAudioSource.mute = false;

                if (!policeAudioSource.isPlaying)
                {
                    policeAudioSource.Play();
                }
            }

            if (lightNum == 1 || lightNum == 3)
            {
                foreach (Light RedLight in RedLights)
                {
                    RedLight.enabled = true;
                }

                foreach (Light BlueLight in BlueLights)
                {
                    BlueLight.enabled = false;
                }
            }

            if (lightNum == 5 || lightNum == 7)
            {
                foreach (Light BlueLight in BlueLights)
                {
                    BlueLight.enabled = true;
                }

                foreach (Light RedLight in RedLights)
                {
                    RedLight.enabled = false;
                }
            }

            if (lightNum == 2 || lightNum == 4 || lightNum == 6 || lightNum == 8)
            {
                foreach (Light BlueLight in BlueLights)
                {
                    BlueLight.enabled = false;
                }

                foreach (Light RedLight in RedLights)
                {
                    RedLight.enabled = false;
                }
            }
        }
    }
}
