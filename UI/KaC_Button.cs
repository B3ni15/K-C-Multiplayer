using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KCM.UI
{
    class KaC_Button
    {
        public Button Button = null;
        private static readonly string[] ButtonPaths =
        {
            "TopLevelUICanvas/TopLevel/Body/ButtonContainer/New",
            "MainMenu/TopLevel/Body/ButtonContainer/New" // fallback for older versions
        };

        public string Name
        {
            get => Button.name;
            set => Button.name = value;
        }

        public string Text
        {
            get => Button.GetComponentInChildren<TextMeshProUGUI>().text;
            set => Button.GetComponentInChildren<TextMeshProUGUI>().text = value;
        }

        public UnityAction OnClick
        {
            set => Button.onClick.AddListener(value);
        }

        public Vector3 LocalPosition
        {
            get => Transform.localPosition;
            set => Transform.localPosition = value;
        }

        public Vector3 Position
        {
            get => Transform.position;
            set => Transform.position = value;
        }

        public Vector3 Size
        {
            get => Transform.localScale;
            set => Transform.localScale = value;
        }

        public GameObject GameObject
        {
            get => Button.gameObject;
        }

        public Transform Transform
        {
            get => GameObject.transform;
        }

        public bool FirstSibling
        {
            set
            {
                if (value)
                    Transform.SetAsFirstSibling();
            }
        }

        public bool LastSibling
        {
            set
            {
                if (value)
                    Transform.SetAsLastSibling();
            }
        }

        public int SiblingIndex
        {
            set => Transform.SetSiblingIndex(value);
        }

        public KaC_Button(Transform parent = null) : this(null, parent) { }

        public KaC_Button(Button b, Transform parent = null)
        {
            var templateButton = ResolveTemplateButton(b);

            if (templateButton == null)
                throw new InvalidOperationException("Template button not found in main menu UI.");

            Button = parent == null
                ? GameObject.Instantiate(templateButton)
                : GameObject.Instantiate(templateButton, parent);

            foreach (Localize Localize in Button.GetComponentsInChildren<Localize>())
                GameObject.Destroy(Localize);

            Button.onClick = new Button.ButtonClickedEvent();
        }

        private static Button ResolveTemplateButton(Button providedButton)
        {
            if (providedButton != null)
                return providedButton;

            foreach (var path in ButtonPaths)
            {
                var transform = Constants.MainMenuUI_T?.Find(path);
                if (transform == null)
                    continue;

                var button = transform.GetComponent<Button>();
                if (button != null)
                {
                    Main.helper?.Log($"Using menu button template at '{path}'.");
                    return button;
                }
            }

            Main.helper?.Log("Failed to find menu button template for KaC_Button.");
            return null;
        }

        public override string ToString()
        {
            PropertyInfo[] _PropertyInfos = Button.GetType().GetProperties();

            var sb = new StringBuilder();

            foreach (var info in _PropertyInfos)
            {
                var value = info.GetValue(Button, null) ?? "(null)";
                sb.AppendLine(info.Name + ": " + value.ToString());
            }

            return sb.ToString();
        }
    }
}
