using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace DSP_Game_Speed
{

    class UIPatch
    {

        public static RectTransform[] speedBtn = new RectTransform[3];
        public static UnityEngine.Color[] old = new Color[3];
        public static Text VersionText;
        public static string textOld;
        public static float curSpeed = 1;

        [HarmonyPatch(typeof(GameMain), "Begin"), HarmonyPostfix]
        public static void PostFix()
        {
            if (GameMain.instance != null)
            {
                Time.timeScale = curSpeed;
                VersionText = GameObject.Find("version-text").GetComponent<Text>();
                textOld = VersionText.text;

                if (GameObject.Find("Game Menu/button-1-bg") && !GameObject.Find("speed-btn-3"))
                {
                    RectTransform parent = GameObject.Find("Game Menu").GetComponent<RectTransform>();
                    RectTransform prefab = GameObject.Find("Game Menu/button-1-bg").GetComponent<RectTransform>();

                    old[0] = prefab.transform.Find("button-1").GetComponent<TranslucentImage>().color; // background color
                    old[1] = prefab.transform.Find("circle").GetComponent<Image>().color; // circle border color
                    old[2] = prefab.transform.Find("button-1/icon").GetComponent<Image>().color; // png color

                    Vector3 newPos = GameObject.Find("Game Menu/button-1-bg").GetComponent<RectTransform>().localPosition;
                    newPos.x += 35f;
                    newPos.y -= 20f;

                    for (int i = 0; i < speedBtn.Length; i++)
                    {
                        // Setup game object
                        speedBtn[i] = GameObject.Instantiate<RectTransform>(prefab);
                        speedBtn[i].gameObject.name = $"speed-{i + 1}-btn";
                        speedBtn[i].GetComponent<UIButton>().tips.tipTitle = $"Game Speed ({i + 1}x)";
                        speedBtn[i].GetComponent<UIButton>().tips.tipText = $"Set game speed to {i + 1} x default";
                        speedBtn[i].GetComponent<UIButton>().tips.delay = 0f;

                        // Setup color for active default color
                       
                        //setup image 
                        speedBtn[i].transform.Find("button-1/icon").GetComponent<Image>().sprite = GetSprite(GameSpeed.speedIcon[i]);

                        // Setup parent and size
                        speedBtn[i].SetParent(parent);
                        speedBtn[i].localScale = new Vector3(0.35f, 0.35f, 0.35f);

                        // Setup position on UI        
                        speedBtn[i].localPosition = newPos;
                        newPos.x += 22f;
                        int temp = i;

                        // Add on click listener
                        // Remove other click(pointer down) events on button -- not implemented fully
                        speedBtn[i].GetComponent<UIButton>().OnPointerDown(null);
                        speedBtn[i].GetComponent<UIButton>().OnPointerEnter(null);
                        speedBtn[i].GetComponent<UIButton>().button.onClick.AddListener(() =>
                        {
                            SetGameSpeed(temp);
                        });
                    }
                }
            }
        }

        public static void SetColors(RectTransform btn, bool active)
        {
            if (active)
            {
                btn.transform.Find("button-1").GetComponent<TranslucentImage>().color = new UnityEngine.Color(0f, 1f, 0f, 1f);
                btn.transform.Find("circle").GetComponent<Image>().color = new UnityEngine.Color(0f, 1f, 0f, 1f);
                btn.transform.Find("button-1/icon").GetComponent<Image>().color = new UnityEngine.Color(0f, 1f, 0f, 1f);
            }
            else
            {
                btn.transform.Find("button-1").GetComponent<TranslucentImage>().color = old[0];
                btn.transform.Find("circle").GetComponent<Image>().color = old[1];
                btn.transform.Find("button-1/icon").GetComponent<Image>().color = old[2];
            }
        }
        public static void SetGameSpeed(int speed)
        { 
            curSpeed = speed + 1;
            Time.timeScale = curSpeed;
            VersionText.text = textOld + "\n" + Time.timeScale.ToString() + "x";
            for (int i = 0; i < speedBtn.Length; i++)
            {
                speedBtn[i].GetComponent<UIButton>().ResetTipDelay();
                SetColors(speedBtn[i], false);
            }
            SetColors(speedBtn[speed], true);

        }

        public static Sprite GetSprite(byte[] buffer)
        {
            Texture2D tex = new Texture2D(48, 48, TextureFormat.RGBA32, false);

            tex.LoadRawTextureData(buffer);
            tex.name = $"speed-icon";
            tex.Apply();

            return Sprite.Create(tex, new Rect(0f, 0f, 48f, 48f), new Vector2(0f, 0f), 1000);

        }


    }
}
