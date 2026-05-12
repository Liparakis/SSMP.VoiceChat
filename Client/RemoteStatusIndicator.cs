using System;
using TMProOld;
using UnityEngine;

namespace SsmpVoiceChat.Client
{
    [RequireComponent(typeof(TextMeshPro))]
    internal class RemoteStatusIndicator : MonoBehaviour
    {
        public bool Talking = false;

        TextMeshPro textComponent;

        float timeout = 0;

        void Awake()
        {
            textComponent = GetComponent<TextMeshPro>();
        }

        void OnEnable()
        {
            UpdateState(false);
        }

        void OnDisable()
        {
            UpdateState(false); 
        }

        void FixedUpdate()
        {
            if (!Talking) return;
            if (timeout > 0)
            {
                timeout -= Time.deltaTime;
                return;
            }

            UpdateState(false);
        }

        public void UpdateState(bool talking)
        {
            Talking = talking;

            if (Talking)
            {
                textComponent.outlineColor = new Color(0.02f, 0.35f, 0);
                timeout = 0.25f;
            }
            else
            {
                textComponent.outlineColor = Color.black;
                timeout = 0;
            }
        }

        public static RemoteStatusIndicator? GetIconOnPlayerContainer(GameObject playerContainer)
        {
            var username = VoiceStatusIcon.FindChild(playerContainer.transform, "Username");
            if (username == null) return null;

            return username.AddComponentIfNotPresent<RemoteStatusIndicator>();
        }
    }
}
