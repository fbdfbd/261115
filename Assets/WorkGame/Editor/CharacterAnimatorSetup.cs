using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace WorkGame.Editor
{
    public static class CharacterAnimatorSetup
    {
        private const string PrefabPath = "Assets/WorkGame/Prefab/Character.prefab";
        private const string Folder = "Assets/WorkGame/Animation/Character";

        [MenuItem("Work Game/Character/Center Mouth And Nose")]
        public static void CenterMouthAndNose()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before editing the prefab.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var head = Required(root.transform, "BreathPivot/HeadPivot");
                var center = Required(head, "Face").localPosition.x;
                foreach (var name in new[] { "Mouth", "Nose" })
                {
                    var part = Required(head, name);
                    var position = part.localPosition;
                    position.x = center;
                    part.localPosition = position;
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log("Character mouth and nose centered on Face; heights, sprites and animations preserved.");
        }

        [MenuItem("Work Game/Character/Center Eye Positions")]
        public static void CenterEyePositions()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before editing the prefab.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var head = Required(root.transform, "BreathPivot/HeadPivot");
                var left = Required(head, "EyeL");
                var right = Required(head, "EyeR");
                var face = Required(head, "Face").localPosition;
                var halfSpacing = (Mathf.Abs(left.localPosition.x - face.x) +
                    Mathf.Abs(right.localPosition.x - face.x)) * 0.5f;
                var height = (left.localPosition.y + right.localPosition.y) * 0.5f;
                left.localPosition = new Vector3(face.x - halfSpacing, height, left.localPosition.z);
                right.localPosition = new Vector3(face.x + halfSpacing, height, right.localPosition.z);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log("Character eye positions centered on Face; source sprites and animation clips preserved.");
        }

        [MenuItem("Work Game/Character/Connect Idle Animator")]
        public static void Connect()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before connecting the prefab.");
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "/CharacterIdle.controller") != null)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Debug.Log("Character Animator already connected; existing animation edits were preserved.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<Animator>() != null || root.transform.Find("BreathPivot") != null)
                    throw new InvalidOperationException("Character already has an Animator or rig; inspect before replacing it.");

                // Keep all source SpriteRenderers, asset pivots and sorting orders intact.
                var body = Required(root.transform, "Body");
                var face = Required(root.transform, "Face");
                var leftEar = Required(root.transform, "LeftEar");
                var rightEar = Required(root.transform, "RightEar");
                var tail = Required(root.transform, "Tail");
                foreach (var name in new[] { "Left", "Right", "BagL", "BagR", "EyeL", "EyeR", "Mouth", "Nose" })
                    Required(root.transform, name);

                var bodyBounds = body.GetComponent<SpriteRenderer>().sprite.bounds;
                var breath = Pivot(root.transform, "BreathPivot",
                    body.localPosition + new Vector3(0f, bodyBounds.min.y, 0f));
                var head = Pivot(breath, "HeadPivot", root.transform.InverseTransformPoint(face.position) - breath.localPosition);
                foreach (var name in new[] { "Face", "EyeL", "EyeR", "Mouth", "Nose" })
                    Required(root.transform, name).SetParent(head, true);
                foreach (var name in new[] { "Body", "Left", "Right", "BagL", "BagR" })
                    Required(root.transform, name).SetParent(breath, true);
                EarPivot(head, leftEar, "LeftEarPivot", true);
                EarPivot(head, rightEar, "RightEarPivot", false);
                var tailBounds = tail.GetComponent<SpriteRenderer>().sprite.bounds;
                var tailPivot = Pivot(root.transform, "TailPivot", tail.localPosition +
                    new Vector3(tailBounds.min.x * 0.7f, tailBounds.min.y * 0.65f, 0f));
                tail.SetParent(tailPivot, true);

                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh();
                var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "/CharacterIdle.controller");
                AddLayer(controller, "Breath", 3.6f, clip =>
                {
                    Curve(clip, "BreathPivot", "m_LocalScale.x", Keys(0f, 1f, 1.8f, 1.006f, 3.6f, 1f));
                    Curve(clip, "BreathPivot", "m_LocalScale.y", Keys(0f, 1f, 1.8f, 1.012f, 3.6f, 1f));
                    Curve(clip, "BreathPivot/HeadPivot", "m_LocalPosition.y",
                        Keys(0f, head.localPosition.y, 2f, head.localPosition.y + 0.035f, 3.6f, head.localPosition.y));
                });
                AddLayer(controller, "Tail", 4.8f, clip => Curve(clip, "TailPivot", "localEulerAnglesRaw.z",
                    Keys(0f, 0f, 1.2f, 3f, 2.4f, 0f, 3.6f, -3f, 4.8f, 0f)));
                AddLayer(controller, "LeftEar", 8f, clip => Curve(clip, "BreathPivot/HeadPivot/LeftEarPivot", "localEulerAnglesRaw.z",
                    Keys(0f, 0f, 2.7f, 0f, 2.82f, -5f, 2.96f, 2f, 3.16f, 0f, 6.1f, 0f, 6.24f, -3f, 6.46f, 0f, 8f, 0f)));
                AddLayer(controller, "RightEar", 8f, clip => Curve(clip, "BreathPivot/HeadPivot/RightEarPivot", "localEulerAnglesRaw.z",
                    Keys(0f, 0f, 3.04f, 0f, 3.17f, 4f, 3.33f, -1.5f, 3.55f, 0f, 6.34f, 0f, 6.49f, 3f, 6.72f, 0f, 8f, 0f)));

                var animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                root.AddComponent<CharacterIdleAnimator>();
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            Verify();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log("Character Animator connected: Breath, Tail, LeftEar, RightEar. Adjust CharacterIdleAnimator in Inspector; edit clips in Animation window.");
        }

        private static Transform Required(Transform root, string name) => root.Find(name) ??
            throw new InvalidOperationException("Missing Character part: " + name);

        private static Transform Pivot(Transform parent, string name, Vector3 position)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            return pivot;
        }

        private static void EarPivot(Transform head, Transform ear, string name, bool left)
        {
            var bounds = ear.GetComponent<SpriteRenderer>().sprite.bounds;
            var attachment = ear.TransformPoint(new Vector3(left ? bounds.max.x * 0.55f : bounds.min.x * 0.55f,
                bounds.min.y * 0.7f, 0f));
            var pivot = Pivot(head, name, head.InverseTransformPoint(attachment));
            ear.SetParent(pivot, true);
        }

        private static void AddLayer(AnimatorController controller, string name, float duration, Action<AnimationClip> animate)
        {
            var motion = new AnimationClip { name = name, frameRate = 60f };
            animate(motion);
            var settings = AnimationUtility.GetAnimationClipSettings(motion);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(motion, settings);
            AssetDatabase.CreateAsset(motion, Folder + "/" + name + ".anim");

            // Matching bindings on both children make strength zero return exactly to the rest pose.
            var rest = new AnimationClip { name = name + "Rest", frameRate = 60f };
            foreach (var binding in AnimationUtility.GetCurveBindings(motion))
            {
                var value = AnimationUtility.GetEditorCurve(motion, binding).Evaluate(0f);
                AnimationUtility.SetEditorCurve(rest, binding, AnimationCurve.Constant(0f, duration, value));
            }
            AnimationUtility.SetAnimationClipSettings(rest, settings);
            AssetDatabase.CreateAsset(rest, Folder + "/" + name + "Rest.anim");

            controller.AddParameter(name + "Strength", AnimatorControllerParameterType.Float);
            controller.AddParameter(name + "Speed", AnimatorControllerParameterType.Float);
            var parameters = controller.parameters;
            for (var i = parameters.Length - 2; i < parameters.Length; i++) parameters[i].defaultFloat = 1f;
            controller.parameters = parameters;
            var layerIndex = name == "Breath" ? 0 : controller.layers.Length;
            if (layerIndex > 0) controller.AddLayer(name);
            var layers = controller.layers;
            layers[layerIndex].name = name;
            layers[layerIndex].defaultWeight = 1f;
            var state = layers[layerIndex].stateMachine.AddState(name + "Idle");
            var tree = new BlendTree { name = name + "Strength", blendType = BlendTreeType.Simple1D,
                blendParameter = name + "Strength", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(rest, 0f);
            tree.AddChild(motion, 1f);
            state.motion = tree;
            state.speedParameter = name + "Speed";
            state.speedParameterActive = true;
            state.writeDefaultValues = false;
            layers[layerIndex].stateMachine.defaultState = state;
            controller.layers = layers;
        }

        private static AnimationCurve Keys(params float[] values)
        {
            var keys = new Keyframe[values.Length / 2];
            for (var i = 0; i < keys.Length; i++) keys[i] = new Keyframe(values[i * 2], values[i * 2 + 1], 0f, 0f);
            return new AnimationCurve(keys);
        }

        private static void Curve(AnimationClip clip, string path, string property, AnimationCurve curve) =>
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);

        [MenuItem("Work Game/Character/Verify Idle Animator")]
        public static void Verify()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var controller = prefab.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
            if (controller == null || controller.layers.Length < 4 || controller.parameters.Length < 8)
                throw new InvalidOperationException("Character Animator layers or parameters are incomplete.");
            var instance = UnityEngine.Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var animator = instance.GetComponent<Animator>();
                animator.Rebind();
                animator.Update(0f);
                var originalPosition = instance.transform.position;
                var originalScale = instance.transform.localScale;
                var renderers = instance.GetComponentsInChildren<SpriteRenderer>();
                var originalSpriteCount = 0;
                var expressions = instance.transform.Find("BreathPivot/HeadPivot/Expressions");
                foreach (var renderer in renderers)
                    if (expressions == null || !renderer.transform.IsChildOf(expressions)) originalSpriteCount++;
                if (originalSpriteCount != 13) throw new InvalidOperationException("Character original sprite count changed.");
                var bindings = 0;
                foreach (var clip in controller.animationClips)
                {
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        if (instance.transform.Find(binding.path) == null)
                            throw new InvalidOperationException("Unresolved animation binding: " + binding.path);
                        bindings++;
                    }
                }
                var breath = instance.transform.Find("BreathPivot");
                var tail = instance.transform.Find("TailPivot");
                animator.Update(1.2f);
                if (Mathf.Abs(tail.localEulerAngles.z) < 0.1f || breath.localScale.y <= 1.001f)
                    throw new InvalidOperationException("Animator did not animate breath and tail.");
                animator.Update(1.62f);
                var ear = instance.transform.Find("BreathPivot/HeadPivot/LeftEarPivot");
                if (Quaternion.Angle(ear.localRotation, Quaternion.identity) < 0.1f)
                    throw new InvalidOperationException("Animator did not animate left ear.");
                animator.Update(0.35f);
                ear = instance.transform.Find("BreathPivot/HeadPivot/RightEarPivot");
                if (Quaternion.Angle(ear.localRotation, Quaternion.identity) < 0.1f)
                    throw new InvalidOperationException("Animator did not animate right ear.");
                foreach (var name in new[] { "Breath", "Tail", "LeftEar", "RightEar" })
                    animator.SetFloat(name + "Strength", 0f);
                animator.Update(0f);
                if (Vector3.Distance(breath.localScale, Vector3.one) > 0.0001f ||
                    Quaternion.Angle(tail.localRotation, Quaternion.identity) > 0.01f)
                    throw new InvalidOperationException("Strength zero did not restore rest pose.");
                if (instance.transform.position != originalPosition || instance.transform.localScale != originalScale)
                    throw new InvalidOperationException("Character root placement changed during animation.");
                File.WriteAllText("Temp/CharacterAnimatorVerification.txt",
                    "PASS: 4 layers, 8 parameters, " + bindings + " resolved curve bindings, 13 sprites; breath/tail/both ears move; strength zero restores rest; root stays fixed.");
                Debug.Log("Character idle animation verification passed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
    }
}
