using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barracuda;
using UnityEngine.UI;

namespace BarracudaSample
{
    public class PredDynamicTexture : MonoBehaviour
    {
        [Header("DL Model Properties")]
        [SerializeField, Tooltip("学習済みモデルのファイル名 (.nn ファイル)")]
        private string m_ModelName;

        private Model m_Model;
        private IWorker m_Worker;

        [Header("Paint Properties")]
        [SerializeField, Tooltip("キャンバスとなるプレーンオブジェクト")]
        private GameObject m_Plane;

        [SerializeField, Tooltip("筆の太さ")]
        private float m_BrushWidth = 2;

        private Texture2D m_DrawTexture;
        private Color[] m_Buffer;

        [Header("UI Properties")]
        [SerializeField, Tooltip("推論結果を表示するテキスト")]
        private Text m_Text;

        // Start is called before the first frame update
        void Start()
        {
            // モデルのロード
            m_Model = ModelLoader.LoadFromStreamingAssets(m_ModelName + ".nn");
            // ワーカーの作成
            m_Worker = BarracudaWorkerFactory.CreateWorker(BarracudaWorkerFactory.Type.ComputePrecompiled, m_Model);

            // テクスチャーペイントの準備
            Texture2D mainTexture = (Texture2D)m_Plane.GetComponent<Renderer>().material.mainTexture;
            Color[] pixels = mainTexture.GetPixels();

            m_Buffer = new Color[pixels.Length];
            pixels.CopyTo(m_Buffer, 0);

            m_DrawTexture = new Texture2D(mainTexture.width, mainTexture.height, TextureFormat.ARGB32, false);
            m_DrawTexture.filterMode = FilterMode.Point;
        }

        // Update is called once per frame
        void Update()
        {
            // 入力し始める際に, テクスチャーを黒で塗りつぶす.
            if(Input.GetMouseButtonDown(0))
            {
                ClearDraw();
            }

            // マウスをドラッグし続けている間は, テクスチャーにお絵かき.
            if(Input.GetMouseButton(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 100.0f))
                {
                    Draw(hit.textureCoord * m_DrawTexture.width);
                }

                m_DrawTexture.SetPixels(m_Buffer);
                m_DrawTexture.Apply();
                m_Plane.GetComponent<Renderer>().material.mainTexture = m_DrawTexture;
            }

            // マウスのドラッグをやめたら, 推論をおこなう.
            if(Input.GetMouseButtonUp(0))
            {
                int pred = Pred();
                m_Text.text = "Pred: " + pred.ToString();
            }
        }

        private void OnDestroy()
        {
            // 後片付け
            m_Worker.Dispose();
        }

        /**
         * @function Pred
         * @abstract 推論をします.
         * @return 推論から導かれた最もそれらしい数値.
         */
        private int Pred()
        {
            // 入力の作成
            var tensor = new Tensor(m_DrawTexture, 1);

            // 推論の実行
            m_Worker.Execute(tensor);

            // 推論結果の取得
            var O = m_Worker.Peek();

            // 推論結果から最もそれらしい数値を取得.
            int pred = 0;
            float maxVal = float.MinValue;
            for(int i = 0; i < 10; ++i)
            {
                if(maxVal < O.readonlyArray[i])
                {
                    pred = i;
                    maxVal = O.readonlyArray[i];
                }
            }

            // 後片付け
            O.Dispose();

            return pred;
        }

        /**
         * @function ClearDraw
         * @abstract テクスチャーを黒く塗りつぶします.
         */
        private void ClearDraw()
        {
            for(int x = 0; x < m_DrawTexture.width; ++x)
            {
                for(int y = 0; y < m_DrawTexture.height; ++y)
                {
                    m_Buffer.SetValue(Color.black, x + m_DrawTexture.width * y);
                }
            }
        }

        /**
         * @function Draw
         * @abstract 座標 p を白く塗ります.
         * @param(p) 白く塗りたい座標.
         */
        private void Draw(Vector2 p)
        {
            for(int x = 0; x < m_DrawTexture.width; ++x)
            {
                for(int y = 0; y < m_DrawTexture.height; ++y)
                {
                    if( (p - new Vector2(x, y)).magnitude < (m_BrushWidth/2) )
                    {
                        m_Buffer.SetValue(Color.white, x + m_DrawTexture.width * y);
                    }
                }
            }
        }
    }
}