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
    public class KCUIElement
    {
        public Transform t = null;

        public string Name
        {
            get => t.name;
            set => t.name = value;
        }

        public string Text
        {
            get => t.GetComponentInChildren<TextMeshProUGUI>().text;
            set => t.GetComponentInChildren<TextMeshProUGUI>().text = value;
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
            get => t.gameObject;
        }

        public Transform Transform
        {
            get => t;
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

        public KCUIElement(Transform e, Transform parent = null)
        {

            if (parent == null)
                t = GameObject.Instantiate(e);
            else
                t = GameObject.Instantiate(e, parent);

            /*foreach (Transform child in e.GetComponentInChildren<Transform>())
            {
                createChild(child, t);
            }*/

            t.gameObject.SetActive(true);

            //foreach (Localize Localize in Button.GetComponentsInChildren<Localize>())
            //    GameObject.Destroy(Localize);

            //Button.onClick = new Button.ButtonClickedEvent();
        }

        public static void createChild(Transform child, Transform parent)
        {
            var t = GameObject.Instantiate(child, parent);

            foreach (Transform c in child.GetComponentInChildren<Transform>())
            {
                createChild(c, t);
            }
        }

        public override string ToString()
        {
            PropertyInfo[] _PropertyInfos = t.GetType().GetProperties();

            var sb = new StringBuilder();

            foreach (var info in _PropertyInfos)
            {
                var value = info.GetValue(Transform, null) ?? "(null)";
                sb.AppendLine(info.Name + ": " + value.ToString());
            }

            return sb.ToString();
        }
    }
}
