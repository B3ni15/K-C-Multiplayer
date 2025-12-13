using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KCM
{
    public class ModalManager
    {
        static GameObject modalInst;
        static bool instantiated = false;

        static TMPro.TextMeshProUGUI tmpTitle;
        static TMPro.TextMeshProUGUI tmpDescription;
        static Button acceptButton;

        static ModalManager()
        {
            if (!instantiated)
            {
                modalInst = GameObject.Instantiate(PrefabManager.modalUIPrefab, Constants.MainMenuUI_T);
                modalInst.SetActive(false);

                acceptButton = modalInst.transform.Find("Modal/Container/Button").GetComponent<UnityEngine.UI.Button>();

                

                tmpTitle = modalInst.transform.Find("Modal/Container/Title").GetComponent<TextMeshProUGUI>();
                tmpDescription = modalInst.transform.Find("Modal/Container/Description").GetComponent<TextMeshProUGUI>();

                instantiated = true;
            }
            else
            {
                throw new Exception("ModalManager is a singleton and may only be instantiated once");
            }
        }

        public static void ShowModal(string title, string message, string buttonText = "Okay", bool withButton = true, Action action = null)
        {
            tmpTitle.text = title;
            tmpDescription.text = message;

            acceptButton.gameObject.SetActive(withButton);

            acceptButton.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = buttonText;

            acceptButton.onClick.RemoveAllListeners();

            acceptButton.onClick.AddListener(() =>
            {
                modalInst.SetActive(false); // Clicked okay
                action?.Invoke();
            });

            modalInst.SetActive(true);
        }

        public static void HideModal()
        {
            modalInst.SetActive(false);
        }
    }
}
