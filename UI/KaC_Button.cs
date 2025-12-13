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

        public KaC_Button(Transform parent = null)
        {
            Button b = Constants.MainMenuUI_T.Find("TopLevelUICanvas/TopLevel/Body/ButtonContainer/New").GetComponent<Button>();

            if (parent == null)
                Button = GameObject.Instantiate(b);
            else
                Button = GameObject.Instantiate(b, parent);

            foreach (Localize Localize in Button.GetComponentsInChildren<Localize>())
                GameObject.Destroy(Localize);

            Button.onClick = new Button.ButtonClickedEvent();
        }

        public KaC_Button(Button b, Transform parent = null)
        {
            if (b == null)
                b = Constants.MainMenuUI_T.Find("TopLevelUICanvas/TopLevel/Body/ButtonContainer/New").GetComponent<Button>();

            if (parent == null)
                Button = GameObject.Instantiate(b);
            else
                Button = GameObject.Instantiate(b, parent);

            foreach (Localize Localize in Button.GetComponentsInChildren<Localize>())
                GameObject.Destroy(Localize);

            Button.onClick = new Button.ButtonClickedEvent();
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
