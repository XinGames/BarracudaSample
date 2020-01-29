using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barracuda;

namespace BarracudaSample
{
    public class TestModel : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("学習済みモデルのファイル名 (.nn ファイル)")]
        // 必ず Assets/StreamingAssets 直下に置くこと.
        private string m_ModelName;

        [SerializeField]
        [Tooltip("入力する文字画像")]
        private Texture2D m_InputTexture;

        // Start is called before the first frame update
        void Start()
        {
            // 動作確認のサンプルなので, すべて Start() 上で実行しています.

            // モデルのロード
            var model = ModelLoader.LoadFromStreamingAssets(m_ModelName + ".nn");

            // ワーカー (推論エンジン) の作成
            var worker = BarracudaWorkerFactory.CreateWorker(BarracudaWorkerFactory.Type.ComputePrecompiled, model);

            // 入力の作成. 第2引数はチャンネル数.
            var tensor = new Tensor(m_InputTexture, 1);

            // 推論の実行
            worker.Execute(tensor);

            // 推論結果の取得
            var O = worker.Peek();

            // 結果の表示
            int pred = 0;
            float maxVal = float.MinValue;
            for (int i = 0; i < 10; ++i)
            {
                if (maxVal < O.readonlyArray[i])
                {
                    pred = i;
                    maxVal = O.readonlyArray[i];
                }
            }
            Debug.Log("Pred: " + pred.ToString());

            // 後片付け (メモリの解放など)
            O.Dispose();
            worker.Dispose();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}