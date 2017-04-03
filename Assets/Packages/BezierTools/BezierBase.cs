using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BezierTools
{
    [System.Serializable]
    public class BezierData
    {
        #region define
        private const int stepsPerCurve = 20;
        #endregion

        #region public
        public ControlPoint[] cps;
        [SerializeField]
        public BezierControlPointMode[] modes;

        /// <summary>
        /// ベジェ曲線の長さ
        /// </summary>
        public float bezierLength = 1;
        public Vector3 bezierBoundingSize = Vector3.zero;
        public Vector3 bezierBoundingCenter = Vector3.zero;

        public float[] bezierCurvesLength;      // ベジェ曲線単位の長さ
        public float[] normalizeBezierPoints;   // 長さを正規化したベジェ曲線の位置
        public float[,] preCalcLinearLength;   // 距離均等に移動するための距離の微分の事前計算配列
        #endregion

        [SerializeField]
        private bool loop = false;

        #region public property
        public bool Loop
        {
            get
            {
                return loop;
            }
            set
            {
                loop = value;
                if (value)
                {
                    Debug.Log("LOOP!");
                    modes[modes.Length - 1] = modes[0];
                    SetControlPoint(0, cps[0].position);
                }
            }
        }

        /// <summary>
        /// ベジェ曲線の座標の数
        /// </summary>
        public int Length
        {
            get
            {
                return (cps.Length - 1) / 3;
            }
        }
        #endregion

        #region public method
        public int GetIndex(int i)
        {
            while (i < 0)
                i += cps.Length;
            return Mathf.Min(i, cps.Length - 1);
        }

        public Vector3 GetCP(int i)
        {
            return cps[GetIndex(i)].position;
        }

        /// <summary>
        /// インデックスから正規化されたパス全体の長さの中での位置(T)を返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public float GetT(int index)
        {
            index = Mathf.Min(index, cps.Length - 1);

            return (float)index / (cps.Length - 1);
        }

        /// <summary>
        /// コントロールポイントのインデックスとその中の時間(T)を取得（コントロールポイント数で等間隔）
        /// </summary>
        /// <param name="inT"></param>
        /// <param name="outIdx"></param>
        /// <param name="outT"></param>
        public void CalcIndex(float inT, out int outIdx, out float outT)
        {
            if (inT >= 1f)
            {
                outT = 1f;
                outIdx = cps.Length - 4;
            }
            else
            {
                outT = Mathf.Clamp01(inT) * Length;
                outIdx = (int)outT;
                outT -= outIdx;
                outIdx *= 3;
            }
        }

        /// <summary>
        /// コントロールポイントのインデックスとその中の時間(T)を取得（ベジェ曲線の長さの割合、移動速度が変わらない）
        /// </summary>
        /// <param name="inT"></param>
        /// <param name="outIdx"></param>
        /// <param name="outT"></param>
        public void CalcIndexNormalize(float inT, out int outIdx, out float outT)
        {
            if (inT >= 1f)
            {
                outT = 1f;
                outIdx = cps.Length - 4;
            }
            else
            {
                outT = inT;
                int i;
                float oldPoint = 0;
                float preOutT = 0;

                //int i;
                //CalcIndex(inT, out i, out preOutT);

                for (i = 0; i < normalizeBezierPoints.Length; i++)
                {
                    if (normalizeBezierPoints[i] >= inT)
                    {
                        preOutT = (inT - oldPoint) / (normalizeBezierPoints[i] - oldPoint);
                        //preOutT = inT / normalizeBezierPoints[i];
                        //Debug.Log("[" + i + "]" + normalizeBezierPoints[i] + " : outT " + outT + " inT " + inT);
                        //Debug.Log("KKKK[" + i + "]" + normalizeBezierPoints[i] + " oldPoint " + oldPoint + " inT " + inT + " preOutT " + preOutT);
                        break;
                    }
                    //Debug.Log("LLLL[" + i + "]" + normalizeBezierPoints[i] + " oldPoint " + oldPoint + " inT " + inT);
                    oldPoint = normalizeBezierPoints[i];
                }

                //outT = Mathf.Clamp01(inT) * Length;
                outIdx = i * 3;
                //outT -= outIdx;
                //outIdx *= 3;
                //outT = CalcLinearLength(i, preOutT);
                //outT = CalcLinearLength(outIdx, preOutT); // 逐次計算
                //outT = preOutT;
                outT = GetPreCalcLinearLength(i, preOutT);  // 事前計算(未完成)
                //Debug.Log("RRRR[" + i + "]" + normalizeBezierPoints[i] + " preOutT " + preOutT + " oldPoint " + oldPoint + " outT " + outT + " inT " + inT + " outIdx " + outIdx + " i " + i);
            }
        }

        static float[] linearLength = { 0, 0, 0, 0,
                                        0, 0, 0, 0,
                                        0, 0, 0, 0,
                                        0, 0, 0, 0,
                                        0, 0, 0, 0 };

        float CalcLinearLength(int index, float t, int divN = 16)
        {
            float ni = 1f / divN;
            float tt = 0;

            //float[,] ll = new float[Length, divN + 1];
            //ll = new float[Length, divN + 1];

            Vector3 p1 = BezierCurve(0, GetCP(index), GetCP(index + 1), GetCP(index + 2), GetCP(index + 3));

            linearLength[0] = 0;

            // パスを分割して距離を保存する
            for (int i = 1; i <= divN; i++)
            {
                tt += ni;
                Vector3 p2 = BezierCurve(tt, GetCP(index), GetCP(index + 1), GetCP(index + 2), GetCP(index + 3));
                linearLength[i] = linearLength[i - 1] + Vector3.Distance(p1, p2);
                //Debug.Log("[" + i + "] index " + index + " linearLength " + linearLength[i]);
                p1 = p2;
            }

            // linearLengthを0～1の範囲に正規化する
            float xx = 1f / linearLength[divN];
            for (int i = 1; i <= divN; i++)
            {
                linearLength[i] *= xx;
            }

            // tを距離として該当するlinearLength区画を探す
            int ii;
            for (ii = 0; ii < divN; ii++)
            {
                if ((t >= linearLength[ii]) && (t <= linearLength[ii + 1]))
                    break;
            }

            if (ii >= divN)
                return t;

            // 線形補間
            xx = (linearLength[ii + 1] - linearLength[ii]);
            if (xx <= float.Epsilon) xx = float.Epsilon;
            xx = (t - linearLength[ii]) / xx;

            return (ii * (1f - xx) + (ii + 1) * xx) * ni;
        }

        float GetPreCalcLinearLength(int index, float t, int divN=16)
        {
            if(preCalcLinearLength == null)
            {
                PreCalcLinearLength(ref preCalcLinearLength);
            }
            float ni = 1f / divN;

            int ii;
            for(ii = 0; ii < divN; ii++)
            {
                //Debug.Log("index " + index + " ii " + ii);
                if ((t >= preCalcLinearLength[index, ii]) && (t <= preCalcLinearLength[index, ii + 1]))
                    break;
            }

            //Debug.Log("index " + index + " ii " + ii);

            if (ii >= divN)
                return t;

            float xx = (preCalcLinearLength[index, ii + 1] - preCalcLinearLength[index, ii]);
            if (xx <= float.Epsilon) xx = float.Epsilon;
            xx = (t - preCalcLinearLength[index, ii]) / xx;

            return (ii * (1f - xx) + (ii + 1) * xx) * ni;
        }

        //float[,] PreCalcLinearLength(int divN = 16)
        void PreCalcLinearLength(ref float[,] ll, int divN = 16)
        {
            //Debug.Log("PreCalcLinearLength");

            float ni = 1f / divN;

            //float[,] ll = new float[Length, divN+1];
            ll = new float[Length, divN + 1];

            for (int i = 0; i < Length; i++)
            {
                float tt = 0;

                int idx = i * 3;
                Vector3 p1 = BezierCurve(0, GetCP(idx), GetCP(idx + 1), GetCP(idx + 2), GetCP(idx + 3));

                ll[i, 0] = 0;

                for (int j = 1; j <= divN; j++)
                {
                    tt += ni;
                    Vector3 p2 = BezierCurve(tt, GetCP(idx), GetCP(idx + 1), GetCP(idx + 2), GetCP(idx + 3));
                    ll[i, j] = ll[i, j - 1] + Vector3.Distance(p1, p2);
                    p1 = p2;
                    //Debug.Log("[" + i + "," + j + "] index " + idx + " linearLength " + ll[i, j]);
                }

                float xx = 1f / ll[i, divN];
                for (int j = 1; j <= divN; j++)
                {
                    ll[i, j] *= xx;
                }
            }
        }

        public Vector3 Position(float t)
        {
            int i;
            CalcIndex(t, out i, out t);
            return BezierCurve(t, GetCP(i), GetCP(i + 1), GetCP(i + 2), GetCP(i + 3));
        }

        /// <summary>
        /// 移動速度が変わらない座標
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Vector3 PositionNormalizeT(float t)
        {
            int i;
            CalcIndexNormalize(t, out i, out t);
            return BezierCurve(t, GetCP(i), GetCP(i + 1), GetCP(i + 2), GetCP(i + 3));
        }

        public Vector3 Velocity(float t)
        {
            int i;
            CalcIndex(t, out i, out t);
            return GetFirstDerivative(t, GetCP(i), GetCP(i + 1), GetCP(i + 2), GetCP(i + 3));
        }

        /// <summary>
        /// 移動速度が変わらないベロシティ
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Vector3 VelocityNormalizeT(float t)
        {
            int i;
            CalcIndexNormalize(t, out i, out t);
            return GetFirstDerivative(t, GetCP(i), GetCP(i + 1), GetCP(i + 2), GetCP(i + 3));
        }

        public Vector3 Direction(float t)
        {
            return Velocity(t).normalized;
        }

        public void AddCurve()
        {
            Vector3 point = cps[cps.Length - 1].position;
            Array.Resize(ref cps, cps.Length + 3);
            point.x += 1f;
            cps[cps.Length - 3] = new ControlPoint();
            cps[cps.Length - 3].position = point;
            point.x += 1f;
            cps[cps.Length - 2] = new ControlPoint();
            cps[cps.Length - 2].position = point;
            point.x += 1f;
            cps[cps.Length - 1] = new ControlPoint();
            cps[cps.Length - 1].position = point;

            Array.Resize(ref modes, modes.Length + 1);
            modes[modes.Length - 1] = modes[modes.Length - 2];
            EnforceMode(cps.Length - 4);

            if (loop)
            {
                cps[cps.Length - 1].position = cps[0].position;
                modes[modes.Length - 1] = modes[0];
                EnforceMode(0);
            }
        }

        public void Reset(float x=0, float y=0, float z=0)
        {
            cps = new ControlPoint[4];
            Vector3 point = new Vector3(x, y, z);
            for (int i = 0; i < 4; i++)
            {
                cps[i] = new ControlPoint();
                cps[i].position = point;
                point.x += 1f;
            }

            modes = new BezierControlPointMode[]
            {
                BezierControlPointMode.Free,
                BezierControlPointMode.Free
            };

            CalcBezierLength();
        }

        public BezierControlPointMode GetControlPointMode(int index)
        {
            return modes[(index + 1) / 3];
        }

        public void SetControlPoint(int index, Vector3 point)
        {
            if (index % 3 == 0)
            {
                Vector3 delta = point - cps[index].position;
                if (loop)
                {
                    if (index == 0)
                    {
                        cps[1].position += delta;
                        cps[cps.Length - 2].position += delta;
                        cps[cps.Length - 1].position = point;
                    }
                    else if (index == cps.Length - 1)
                    {
                        cps[0].position = point;
                        cps[1].position += delta;
                        cps[index - 1].position += delta;
                    }
                    else
                    {
                        cps[index - 1].position += delta;
                        cps[index + 1].position += delta;
                    }
                }
                else
                {
                    if (index > 0)
                    {
                        cps[index - 1].position += delta;
                    }
                    if (index + 1 < cps.Length)
                    {
                        cps[index + 1].position += delta;
                    }
                }
            }
            cps[index].position = point;
            EnforceMode(index);
        }

        public void SetControlPointMode(int index, BezierControlPointMode mode)
        {
            int modeIndex = (index + 1) / 3;
            modes[modeIndex] = mode;
            if (loop)
            {
                if (modeIndex == 0)
                {
                    modes[modes.Length - 1] = mode;
                }
                else if (modeIndex == modes.Length - 1)
                {
                    modes[0] = mode;
                }
            }
            EnforceMode(index);

        }

        public static Vector3 GetFirstDerivative(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            t = Mathf.Clamp01(t);
            float onMinusT = 1f - t;

            return 3f * onMinusT * onMinusT * (p1 - p0) + 6f * onMinusT * t * (p2 - p1) + 3f * t * t * (p3 - p2);
        }

        public BezierData Copy()
        {
            BezierData b = new BezierData();
            b.cps = new ControlPoint[cps.Length];
            for (int i = 0; i < this.cps.Length; i++)
            {
                b.cps[i] = new ControlPoint();
                b.cps[i].position.x = cps[i].position.x;
                b.cps[i].position.y = cps[i].position.y;
                b.cps[i].position.z = cps[i].position.z;
            }
            b.modes = new BezierControlPointMode[this.modes.Length];
            for (int i = 0; i < this.modes.Length; i++)
            {
                b.modes[i] = this.modes[i];
            }
            b.loop = loop;
            return b;
        }

        public void SetOffset(Vector3 offset)
        {
            for(int i = 0; i < cps.Length; i++)
            {
                cps[i].position += offset;
            }
        }

        public void SetOffsetScale(Vector3 offset)
        {
            for (int i = 0; i < cps.Length; i++)
            {
                cps[i].position.x *= offset.x;
                cps[i].position.y *= offset.y;
                cps[i].position.z *= offset.z;
            }
        }

        public Vector3 GetSize()
        {
            Vector3 min = Vector3.one * 1000000f;
            Vector3 max = Vector3.one * -1000000f;
            for(int i = 0; i < Length; i += 3)
            {
                if (min.x > cps[i].position.x) min.x = cps[i].position.x;
                if (min.y > cps[i].position.y) min.y = cps[i].position.y;
                if (min.z > cps[i].position.z) min.z = cps[i].position.z;

                if (max.x < cps[i].position.x) max.x = cps[i].position.x;
                if (max.y < cps[i].position.y) max.y = cps[i].position.y;
                if (max.z < cps[i].position.z) max.z = cps[i].position.z;
            }

            return new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), Mathf.Abs(max.z - min.z));
        }

        /// <summary>
        /// ベジェデータを末尾に付け足す
        /// </summary>
        public void AddBezierData(BezierData data)
        {
            //Vector3 point = cps[cps.Length - 1].position;
            int oldLast = cps.Length - 1;
            int oldModeIndex = modes.Length - 1;
            int mergeOffset = 2;
            int dataLength = data.cps.Length;

            // 末端の制御点を保持
            Vector3 oldLastControlPos = cps[oldLast - 1].position;

            Array.Resize(ref cps, cps.Length + dataLength + mergeOffset);  // 結合用パスの制御点２つ追加
            Array.Resize(ref modes, modes.Length + mergeOffset + data.modes.Length);

            // データコピー
            for (int i = 0; i < dataLength; i++)
            {
                int idx = oldLast + mergeOffset + i + 1;
                //Debug.Log("[" + i + "] cps[" + (idx) + "] data.cps["+i+"].position " + data.cps[i].position);
                cps[idx] = new ControlPoint();
                cps[idx].position = data.cps[i].position;
            }
            for(int i = 0; i < data.modes.Length; i++)
            {
                modes[oldModeIndex + mergeOffset + i] = data.modes[i];
            }

            cps[oldLast + 1] = new ControlPoint();
            cps[oldLast + 2] = new ControlPoint();

            SetControlPointMode(oldLast + 1, BezierControlPointMode.Aligned);
            SetControlPointMode(oldLast + 2, BezierControlPointMode.Aligned);

            // 間の制御点の座標を前後の制御点からの流れに沿わせる
            Vector3 p1 = cps[oldLast].position + (cps[oldLast].position - cps[oldLast - 1].position).normalized * 0.01f;
            SetControlPoint(oldLast + 1, p1);

            Vector3 p2 = cps[oldLast + 3].position + (cps[oldLast + 3].position - cps[oldLast + 4].position).normalized * 0.01f;
            SetControlPoint(oldLast + 2, p2);

            // 結合前の末端の制御点を復元
            SetControlPoint(oldLast - 1, oldLastControlPos);

        }

        /// <summary>
        /// 選択インデックスの後ろに挿入
        /// </summary>
        /// <param name="index"></param>
        public void InsertNextPoint(int index)
        {
            if(index == (cps.Length - 1))
            {
                // 末端に追加
                AddCurve();
            }
            else
            {
                // 次に追加
                // 中間座標取得
                float d = 1f / (float)(cps.Length - 1);
                float t1 = (float)index * d;
                float t2 = (float)(index + 3) * d;
                float tt = t1 + (t2 - t1) * 0.5f;
                Vector3 middlePoint = Position(tt);
                Vector3 middleVelocity = Velocity(tt) * 0.01f;
                ControlPoint[] newCps = new ControlPoint[3];

                newCps[0] = new ControlPoint();
                newCps[0].position = middlePoint - middleVelocity;
                newCps[1] = new ControlPoint();
                newCps[1].position = middlePoint;
                newCps[2] = new ControlPoint();
                newCps[2].position = middlePoint + middleVelocity;
                BezierTools.ArrayUtil.Insert(ref cps, newCps, index + 2);

                int modeIndex = (index + 1) / 3;
                BezierTools.ArrayUtil.Insert(ref modes, BezierControlPointMode.Free, modeIndex + 1);
            }

            if (loop)
            {
                cps[cps.Length - 1].position = cps[0].position;
                modes[modes.Length - 1] = modes[0];
                EnforceMode(0);
            }
        }

        /// <summary>
        /// 選択インデックスを削除
        /// </summary>
        /// <param name="index"></param>
        public void RemovePoint(int index)
        {
            if (index == 0)
            {
                BezierTools.ArrayUtil.Remove(ref cps, 0, 2);
            }
            else if (index == (Length - 1))
            {
                BezierTools.ArrayUtil.Remove(ref cps, index - 2, index);
            }
            else
            {
                BezierTools.ArrayUtil.Remove(ref cps, index - 1, index + 1);
            }

            int modeIndex = (index + 1) / 3;
            BezierTools.ArrayUtil.Remove(ref modes, modeIndex);

            if (Loop)
            {
                Loop = true;
            }
        }

        /// <summary>
        /// 長さを計算
        /// </summary>
        public float CalcBezierLength()
        {
            float len = 0;
            float lenSum = 0;
            Vector3 old = Position(0);
            Vector3 point;

            bezierCurvesLength = new float[Length];
            normalizeBezierPoints = new float[Length];
            //preCalcLinearLength = new float[Length, stepsPerCurve];
            for (int i = 0; i < bezierCurvesLength.Length; i++)
            {
                float startT = GetT(i * 3);
                float endT = GetT((i + 1) * 3);
                float diffT = (endT - startT) / stepsPerCurve;
                len = 0;
                for (int j = 0; j <= stepsPerCurve; j++)
                {
                    point = Position(startT + diffT * j);
                    len += Vector3.Distance(old, point);
                    old = point;
                }
                bezierCurvesLength[i] = len;
                lenSum += len;
                normalizeBezierPoints[i] = lenSum;
            }

            bezierLength = lenSum;

            for(int i = 0; i < normalizeBezierPoints.Length; i++)
            {
                normalizeBezierPoints[i] /= bezierLength;
                //Debug.Log("normalizeBezierPoints[" + i + "] " + normalizeBezierPoints[i]);
            }

            //preCalcLinearLength = PreCalcLinearLength();
            PreCalcLinearLength(ref preCalcLinearLength);

            //int steps = stepsPerCurve * Length;
            //for (int i = 1; i <= steps; i++)
            //{
            //    point = Position((float)i / (float)steps);
            //    len += Vector3.Distance(old, point);
            //    old = point;
            //}

            return bezierLength;
        }

        public void CaleBezierBoundingBox()
        {

            // X
            float left = float.MaxValue;
            float right = float.MinValue;
            // Y
            float bottom = float.MaxValue;
            float top = float.MinValue;
            // Z
            float back = float.MaxValue;
            float forward = float.MinValue;
            
            int steps = stepsPerCurve * Length;
            for (int i = 1; i <= steps; i++)
            {
                Vector3 pos = Position((float)i / (float)steps);
                //Debug.Log("[" + i + "]" + pos);
                if (left >= pos.x) left = pos.x;
                if (right <= pos.x) right = pos.x;

                if (bottom >= pos.y) bottom = pos.y;
                if (top <= pos.y) top = pos.y;

                if (back >= pos.z) back = pos.z;
                if (forward <= pos.z) forward = pos.z;
            }

            //for (int i = 1; i < cps.Length; i += 3)
            //{
            //    Vector3 pos = cps[i].position;
            //    Debug.Log("[" + i + "]" + pos);
            //    if (left >= pos.x) left = pos.x;
            //    if (right <= pos.x) right = pos.x;

            //    if (bottom >= pos.y) bottom = pos.y;
            //    if (top <= pos.y) top = pos.y;

            //    if (back >= pos.z) back = pos.z;
            //    if (forward <= pos.z) forward = pos.z;
            //}

            bezierBoundingSize.x = (right - left);
            bezierBoundingSize.y = (top - bottom);
            bezierBoundingSize.z = (forward - back);

            bezierBoundingCenter.x = left + bezierBoundingSize.x * 0.5f;
            bezierBoundingCenter.y = bottom + bezierBoundingSize.y * 0.5f;
            bezierBoundingCenter.z = back + bezierBoundingSize.z * 0.5f;
            Debug.Log("left " + left + " right " + right + " top " + top + " bottom " + bottom + " forward " + forward + " back " + back);
            Debug.Log("size " + bezierBoundingSize + " center " + bezierBoundingCenter);

            //bezierBoundingSize = GetSize();
            //Debug.Log("size " + bezierBoundingSize + " center " + bezierBoundingCenter);
        }
        #endregion

        #region private method
        private void EnforceMode(int index)
        {
            int modeIndex = (index + 1) / 3;
            BezierControlPointMode mode = modes[modeIndex];
            if (mode == BezierControlPointMode.Free || !loop && (modeIndex == 0 || modeIndex == modes.Length - 1))
            {
                return;
            }

            int middleIndex = modeIndex * 3;
            int fixedIndex, enforcedIndex;
            if (index <= middleIndex)
            {
                fixedIndex = middleIndex - 1;
                if (fixedIndex < 0)
                {
                    fixedIndex = cps.Length - 2;
                }
                enforcedIndex = middleIndex + 1;
                if (enforcedIndex >= cps.Length)
                {
                    enforcedIndex = 1;
                }
            }
            else
            {
                fixedIndex = middleIndex + 1;
                if (fixedIndex >= cps.Length)
                {
                    fixedIndex = 1;
                }
                enforcedIndex = middleIndex - 1;
                if (enforcedIndex < 0)
                {
                    enforcedIndex = cps.Length - 2;
                }
            }

            Vector3 middle = cps[middleIndex].position;
            Vector3 enforcedTangent = middle - cps[fixedIndex].position;
            if (mode == BezierControlPointMode.Aligned)
            {
                enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, cps[enforcedIndex].position);
            }
            cps[enforcedIndex].position = middle + enforcedTangent;
        }

        Vector3 calcVec3;
        private Vector3 BezierCurve(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            t = Mathf.Clamp01(t);
            //float onMinusT = 1f - t;
            double tt = (double)t;
            double onMinusT = 1d - tt;
            double p0x = p0.x;
            double p0y = p0.y;
            double p0z = p0.z;

            double p1x = p1.x;
            double p1y = p1.y;
            double p1z = p1.z;

            double p2x = p2.x;
            double p2y = p2.y;
            double p2z = p2.z;

            double p3x = p3.x;
            double p3y = p3.y;
            double p3z = p3.z;

            double mt3 = onMinusT * onMinusT * onMinusT;
            double mt2 = onMinusT * onMinusT;

            double tt2 = tt * tt;
            double tt3 = tt * tt * tt;

            double bx = mt3 * p0x + 3d * mt2 * tt * p1x + 3d * onMinusT * tt2 * p2x + tt3 * p3x;
            double by = mt3 * p0y + 3d * mt2 * tt * p1y + 3d * onMinusT * tt2 * p2y + tt3 * p3y;
            double bz = mt3 * p0z + 3d * mt2 * tt * p1z + 3d * onMinusT * tt2 * p2z + tt3 * p3z;

            calcVec3.x = (float)bx;
            calcVec3.y = (float)by;
            calcVec3.z = (float)bz;

            return calcVec3;
            //return new Vector3((float)bx, (float)by, (float)bz);
            //return onMinusT * onMinusT * onMinusT * p0 + 3f * onMinusT * onMinusT * t * p1 + 3f * onMinusT * t * t * p2 + t * t * t * p3;
        }
        #endregion

#if UNITY_EDITOR
        #region inspecterGUI
        public bool DrawInspectorGUI(int selectedIndex, ref bool isDrawDirection)
        {
            bool isChange = false;

            if (cps != null)
            {
                EditorGUILayout.LabelField("Bezier Length: " + bezierLength);

                EditorGUI.BeginChangeCheck();
                bool loop = EditorGUILayout.Toggle("Loop", Loop);
                if (EditorGUI.EndChangeCheck())
                {
                    Loop = loop;
                    isChange = true;
                }

                EditorGUI.BeginChangeCheck();
                isDrawDirection = EditorGUILayout.Toggle("DrawDirection", isDrawDirection);
                if (EditorGUI.EndChangeCheck())
                {
                    isChange = true;
                }

                if ((selectedIndex >= 0) && (selectedIndex < cps.Length))
                {
                    isChange |= DrawSelectedPointInspector(selectedIndex);
                }
                EditorGUILayout.BeginHorizontal();
                if ((selectedIndex >= 0) && (selectedIndex < cps.Length) && ((selectedIndex % 3) == 0))
                {
                    if (GUILayout.Button("Add Point"))
                    {
                        Debug.Log("selectedIndex " + selectedIndex);
                        //AddCurve();
                        InsertNextPoint(selectedIndex);
                        isChange = true;
                    }

                    if (cps.Length > 4)
                    {
                        if (GUILayout.Button("Remove Point"))
                        {
                            Debug.Log("selectedIndex " + selectedIndex);
                            RemovePoint(selectedIndex);
                            isChange = true;
                        }
                    }
                }
                if (GUILayout.Button("Reset"))
                {
                    Reset();
                    isChange = true;
                }
                EditorGUILayout.EndHorizontal();

                if (bezierCurvesLength != null)
                {
                    EditorGUILayout.BeginVertical();
                    GUILayout.Label("Bezier Curves Length");
                    for (int i = 0; i < bezierCurvesLength.Length; i++)
                    {
                        GUILayout.Label("[" + i + "] " + bezierCurvesLength[i] + " : " + normalizeBezierPoints[i]);
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            // 変更があったら長さを再計算
            if (isChange)
            {
                CalcBezierLength();
            }
            return isChange;
        }

        private bool DrawSelectedPointInspector(int selectedIndex)
        {
            bool isChange = false;

            GUILayout.Label("Selected Point");
            EditorGUI.BeginChangeCheck();
            Vector3 point = EditorGUILayout.Vector3Field("Position", cps[selectedIndex].position);
            if (EditorGUI.EndChangeCheck())
            {
                isChange = true;
                SetControlPoint(selectedIndex, point);
            }
            EditorGUI.BeginChangeCheck();
            BezierControlPointMode mode = (BezierControlPointMode)EditorGUILayout.EnumPopup("Mode", GetControlPointMode(selectedIndex));
            if (EditorGUI.EndChangeCheck())
            {
                SetControlPointMode(selectedIndex, mode);
                isChange = true;
            }

            // 変更があったら長さを再計算
            if (isChange)
            {
                CalcBezierLength();
            }
            return isChange;
        }
        #endregion

        #region sceneGUI
        private const float handleSize = 0.04f;
        private const float pickSize = 0.06f;
        private const float directionScale = 0.5f;

        private static Color[] modeColors = {
            Color.green,
            Color.yellow,
            Color.cyan
        };

        private Vector3 ShowPoint(int index, ref int selectedIndex, ref bool isChange)
        {
            Vector3 point = cps[index].position;
            var rot = Quaternion.identity;
            float size = HandleUtility.GetHandleSize(point);
            if ((index % 3) == 0)
            {
                size *= 2f;
            }
            Handles.Label(point, "[" + index + "]");
            Handles.color = modeColors[(int)GetControlPointMode(index)];
            if (Handles.Button(point, rot, size * handleSize, size * pickSize, Handles.DotCap))
            {
                selectedIndex = index;
                isChange |= true;
            }


            if (selectedIndex == index)
            {
                EditorGUI.BeginChangeCheck();
                point = Handles.DoPositionHandle(point, rot);
                if (EditorGUI.EndChangeCheck())
                {
                    SetControlPoint(index, point);
                    isChange |= true;
                }
            }
            return point;
        }

        private void ShowDirections()
        {
            Handles.color = Color.green;
            Vector3 point = Position(0f);
            Handles.DrawLine(point, point + Direction(0f) * directionScale);
            int steps = stepsPerCurve * Length;
            for (int i = 1; i <= steps; i++)
            {
                point = Position(i / (float)steps);
                Handles.DrawLine(point, point + Direction(i / (float)steps) * directionScale);
            }
        }

        public bool DrawSceneGUI(ref int selectedIndex, ref bool isShowDirection)
        {
            bool isChange = false;
            
            if (cps != null && cps.Length > 3)
            {
                Vector3 p0 = ShowPoint(0, ref selectedIndex, ref isChange);
                float colStep = 1f / cps.Length;
                for (int i = 1; i < cps.Length; i += 3)
                {
                    Vector3 p1 = ShowPoint(i, ref selectedIndex, ref isChange);
                    Vector3 p2 = ShowPoint(i + 1, ref selectedIndex, ref isChange);
                    Vector3 p3 = ShowPoint(i + 2, ref selectedIndex, ref isChange);

                    Handles.color = Color.gray;
                    Handles.DrawLine(p0, p1);
                    Handles.DrawLine(p2, p3);
                    //Gizmos.color = Color.HSVToRGB(i, 1f, 1f);
                    //Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
                    Handles.DrawBezier(p0, p3, p1, p2, Color.HSVToRGB((float)i * colStep, 1f, 1f), null, 2f);
                    p0 = p3;
                }

                if(isShowDirection) ShowDirections();
            }

            // 変更があったら長さを再計算
            if (isChange)
            {
                CalcBezierLength();
            }

            return isChange;
        }
        #endregion
#endif

        [System.Serializable]
        public class ControlPoint
        {
            public Vector3 position;
        }

    }

    public enum BezierControlPointMode
    {
        Free,
        Aligned,
        Mirrored
    }
}