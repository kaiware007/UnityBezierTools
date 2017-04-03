using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BezierTools
{
    [CustomEditor(typeof(Bezier))]
    public class BezierEditor : Editor
    {
        #region define
        private const float handleSize = 0.04f;
        private const float pickSize = 0.06f;
        private const int stepsPerCurve = 20;
        private const float directionScale = 0.5f;
        #endregion

        private Bezier bezier;
        private int selectedIndex = -1;

        private bool isDrawDirection = false;

        #region publicmethod
        public override void OnInspectorGUI()
        {
            bezier = (Bezier)target;
            if (bezier.data == null)
            {
                bezier.data = new BezierData();
            }
            if (bezier.data.cps == null)
            {
                bezier.data.Reset();
            }

            if (bezier.data.DrawInspectorGUI(selectedIndex, ref isDrawDirection))
            {
                EditorUtility.SetDirty(bezier);
            }
        }
        #endregion

        #region privatemthod
        private void OnSceneGUI()
        {
            bezier = (Bezier)target;
            if (bezier.data == null)
            {
                bezier.data = new BezierData();
            }

            if(bezier.data.DrawSceneGUI(ref selectedIndex, ref isDrawDirection))
            {
                EditorUtility.SetDirty(bezier);
            }
        }

        private void OnEnable()
        {
            SceneView.onSceneGUIDelegate += DrawSceneGUI;
        }
        private void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= DrawSceneGUI;
        }

        private void DrawSceneGUI(SceneView sceneView)
        {
            OnSceneGUI();
        }
        #endregion

        [MenuItem("Assets/Create/Bezier")]
        public static void CreateSpline() { ScriptableObjUtil.CreateAsset<Bezier>(); }
    }

}