using GlobalEnums;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SsmpVoiceChat.Client
{
    internal class VoiceStatusIcon
    {
        const int IMAGE_SIZE = 66;
        Sprite Unmuted;
        //Sprite Muted;
        //Sprite MicNotFound;
        GameObject? MicrophoneIcons;
        SpriteRenderer TalkingIndicator;
        Status CurrentStatus = Status.NotTalking;
        public VoiceStatusIcon() {
            CreateSprites();
            CreateIconObject();

            VoiceChatMod.ModSettings.OnTalkingIndicatorToggled += OnIndicatorToggled;
            OnIndicatorToggled();
        }

        GameObject FindChild(Transform currentObject, string path) {
            var objectNames = path.Split('/');

            foreach (var name in objectNames) {
                var children = currentObject.childCount;

                for (int i = 0; i < children; i++) {
                    var child = currentObject.GetChild(i);
                    if (child.name == name) {
                        currentObject = child;
                        break;
                    }
                }
            }

            return currentObject.gameObject;
        }

        void CreateIconObject() {
            var hud = GameCameras.instance.hudCamera.transform;
            var extras = FindChild(hud, "In-game/Anchor TL/Hud Canvas Offset/Hud Canvas/Extras").transform;

            // Find other top left UI elements
            var thread = FindChild(extras.parent, "Thread/Spool/Thread Spool/Parent/Extender Tool/Extender Sprite");
            var reserveSprite = FindChild(extras, "Reserve Bind/Reserve Bind Sprite");
            var lavaBellSprite = FindChild(extras, "Lava Bell HUD/lava_bell_icon");
            var maggotSprite = FindChild(extras, "Maggot Charm/Maggot Charm Sprite");

            var toolSprite = "Parent/Canvas/Background Image/Radial Image";
            var upToolSprite = FindChild(extras.parent, $"Tool Icons/Tool Icon U/{toolSprite}");
            var neutToolSprite = FindChild(extras.parent, $"Tool Icons/Tool Icon N/{toolSprite}");
            var downToolSprite = FindChild(extras.parent, $"Tool Icons/Tool Icon D/{toolSprite}");

            // Create icon game object
            MicrophoneIcons = new GameObject("Microphone Icons");
            MicrophoneIcons.transform.SetParent(extras, false);
            MicrophoneIcons.transform.localPosition = new Vector3(6.82f, -1.35f, 0);
            MicrophoneIcons.layer = (int)PhysLayers.UI;

            // Add a PositionRelativeTo to offset position based on other active UI elements
            var relative = MicrophoneIcons.AddComponent<PositionRelativeTo>();
            var referenceRelative = reserveSprite.transform.parent.GetComponent<PositionRelativeTo>();

            relative.inSpace = referenceRelative.inSpace;
            relative.target = referenceRelative.target;
            relative.positionX = true;
            relative.offset = new Vector3(1.35f, 0, 0);

            relative.extensions = [
                new PositionRelativeTo.ExtensionPair { AddOffset = new Vector3(0.54f, 0, 0), Target = thread },
                new PositionRelativeTo.ExtensionPair { AddOffset = new Vector3(1.33f, 0, 0), Target = reserveSprite.gameObject },
                new PositionRelativeTo.ExtensionPair { AddOffset = new Vector3(1.2f, 0, 0), Target = lavaBellSprite },
                new PositionRelativeTo.ExtensionPair { AddOffset = new Vector3(1.2f, 0, 0), Target = maggotSprite },
                new PositionRelativeTo.ExtensionPair { AddOffset = new Vector3(1.34f, 0, 0), Target = upToolSprite },
                new PositionRelativeTo.ExtensionPair { AddOffset = new Vector3(1.2f, 0, 0), Target = neutToolSprite },
                new PositionRelativeTo.ExtensionPair { AddOffset = new Vector3(1.2f, 0, 0), Target = downToolSprite }
            ];

            // Set the sprite
            var sprite = MicrophoneIcons.AddComponent<SpriteRenderer>();
            sprite.sprite = Unmuted;
            sprite.sortingLayerName = "Over";
            sprite.sortingOrder = 1;

            // Create speaking indicator
            var child = new GameObject("Speaking Indicator");
            child.transform.SetParentReset(MicrophoneIcons.transform);
            child.transform.localPosition = new Vector3(0, 0, 0.23f);
            child.layer = (int)PhysLayers.UI;

            TalkingIndicator = child.AddComponent<SpriteRenderer>();
            TalkingIndicator.sprite = neutToolSprite.GetComponent<Image>().sprite;
            TalkingIndicator.sortingLayerName = "Over";
            sprite.sortingOrder = 0;
            SetTalking(Status.NotTalking);
        }

        public void DestroyIcon()
        {
            if (MicrophoneIcons != null)
            {
                GameObject.Destroy(MicrophoneIcons);
                MicrophoneIcons = null;
                VoiceChatMod.ModSettings.OnTalkingIndicatorToggled -= OnIndicatorToggled;
            }
        }

        void OnIndicatorToggled()
        {
            if (MicrophoneIcons == null) return;
            var setting = VoiceChatMod.ModSettings.TalkingIndicator;
            MicrophoneIcons.SetActive(setting);
        }

        public enum Status {
            Talking,
            Muted,
            NotTalking,
            Error
        }

        public void SetTalking(Status talking) {
            if (talking == CurrentStatus) return;
            CurrentStatus = talking;

            if (talking == Status.Talking) {
                TalkingIndicator.color = new Color(0.3f, 0.5f, 0.3f, 1);
            } else if (talking == Status.NotTalking) {
                TalkingIndicator.color = new Color(0.2f, 0.2f, 0.2f, 1);
            } else if (talking == Status.Muted) {
                TalkingIndicator.color = new Color(0.9f, 0.17f, 0.15f, 1);
            } else {
                TalkingIndicator.color = new Color(0.5f, 0.15f, 0.9f, 1);
            }
        }

        void CreateSprites() {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var image = Path.Combine(dir, "microphone_icons.png");

            byte[] imageData = File.ReadAllBytes(image);
            Texture2D texture = new Texture2D(IMAGE_SIZE * 3, IMAGE_SIZE);
            texture.LoadImage(imageData);

            var pivot = new Vector2(0.5f, 0.5f);
            Unmuted = Sprite.Create(texture, new Rect(0, 0, IMAGE_SIZE, IMAGE_SIZE), pivot);
            //Muted = Sprite.Create(texture, new Rect(IMAGE_SIZE, 0, IMAGE_SIZE, IMAGE_SIZE), pivot);
            //MicNotFound = Sprite.Create(texture, new Rect(IMAGE_SIZE * 2, 0, IMAGE_SIZE, IMAGE_SIZE), pivot);
        }
    }
}
