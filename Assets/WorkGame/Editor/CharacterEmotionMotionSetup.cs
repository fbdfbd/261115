using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WorkGame.Editor
{
    public static class CharacterEmotionMotionSetup
    {
        private const string Root = "Assets/WorkGame/Animation/Character";
        private const string Folder = Root + "/EmotionMotions";
        private const string Prefab = "Assets/WorkGame/Prefab/Character.prefab";
        private const string Head = "BreathPivot/HeadPivot";
        private static readonly string[] Parts = { "Tail", "LeftEar", "RightEar" };
        private static readonly string[] Emotions = { "Normal", "Cry", "Angry", "Happy" };
        private static string PathFor(string part) => part == "Tail" ? "TailPivot" : Head + "/" + part + "Pivot";

        [MenuItem("Work Game/Character/Apply Emotion Ear And Tail Motions")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before editing animations.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "/CharacterIdle.controller");
            if (controller.layers.Any(layer => layer.stateMachine.states.Any(state => state.state.name == "CryTail")))
                throw new InvalidOperationException("Emotion ear/tail motions already exist; existing edits were preserved.");
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            foreach (var part in Parts)
            {
                var layer = controller.layers.Single(x => x.name == part);
                var machine = layer.stateMachine;
                var normal = machine.defaultState;
                Transition(machine, normal, 0);
                // Reset scale when returning from the flattened angry ears.
                foreach (var suffix in new[] { "", "Rest" })
                {
                    var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/" + part + suffix + ".anim");
                    Scale(idle, PathFor(part), 1f, 1f);
                    EditorUtility.SetDirty(idle);
                }
                for (var emotion = 1; emotion <= 3; emotion++)
                {
                    var duration = emotion == 1 ? 2.8f : emotion == 2 ? 3.2f : 3.6f;
                    var motion = MakeClip(part, emotion, duration, false);
                    var rest = MakeClip(part, emotion, duration, true);
                    AssetDatabase.CreateAsset(motion, Folder + "/" + motion.name + ".anim");
                    AssetDatabase.CreateAsset(rest, Folder + "/" + rest.name + ".anim");
                    var tree = new BlendTree { name = motion.name + "Strength", blendType = BlendTreeType.Simple1D,
                        blendParameter = part + "Strength", useAutomaticThresholds = false };
                    AssetDatabase.AddObjectToAsset(tree, controller);
                    tree.AddChild(rest, 0f);
                    tree.AddChild(motion, 1f);
                    var state = machine.AddState(motion.name, new Vector3(260, emotion * 85));
                    state.motion = tree;
                    state.speedParameter = "ExpressionSpeed";
                    state.speedParameterActive = true;
                    state.writeDefaultValues = false;
                    Transition(machine, state, emotion);
                }
            }
            foreach (var name in new[] { "Angry", "Happy" })
                foreach (var suffix in new[] { "", "Rest" })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/Expressions/" + name + suffix + ".anim");
                    Curve(clip, Head, "localEulerAnglesRaw.z", AnimationCurve.Constant(0, clip.length, 0));
                    Curve(clip, Head + "/Expressions", "m_LocalPosition.y", AnimationCurve.Constant(0, clip.length, 0));
                    EditorUtility.SetDirty(clip);
                }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Verify();
            Debug.Log("Character emotion motions applied: crying ear tremble/drooped tail, fixed angry erect ears/tail, happy ear perks/stronger tail sway. Angry/Happy head shake removed.");
        }

        private static void Transition(AnimatorStateMachine machine, AnimatorState state, int expression)
        {
            var transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = .22f;
            transition.canTransitionToSelf = false;
            transition.interruptionSource = TransitionInterruptionSource.DestinationThenSource;
            transition.AddCondition(AnimatorConditionMode.Equals, expression, "Expression");
        }

        private static AnimationClip MakeClip(string part, int emotion, float duration, bool rest)
        {
            var clip = new AnimationClip { name = Emotions[emotion] + part + (rest ? "Rest" : ""), frameRate = 60 };
            var path = PathFor(part);
            var keys = new Keyframe[169];
            for (var index = 0; index < keys.Length; index++)
            {
                var t = index / (float)(keys.Length - 1);
                var angle = 0f;
                if (!rest)
                {
                    if (part == "Tail")
                        angle = emotion == 1 ? -24f : emotion == 2 ? 22f : 5.5f * Mathf.Sin(t * Mathf.PI * 2);
                    else
                    {
                        var direction = part == "LeftEar" ? -1f : 1f;
                        if (emotion == 1) angle = direction * (-4f + 1.3f * Mathf.Sin(t * Mathf.PI * 36));
                        else if (emotion == 2) angle = direction * 12f;
                        else
                        {
                            // Three soft perks per loop, with a small left/right delay.
                            var phase = Mathf.Repeat(t * 3f - (part == "RightEar" ? .13f : 0f), 1f);
                            var pulse = phase < .58f ? Mathf.Pow(Mathf.Sin(phase / .58f * Mathf.PI), 2f) : 0f;
                            angle = direction * 7f * pulse;
                        }
                    }
                }
                keys[index] = new Keyframe(t * duration, angle);
            }
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < keys.Length; index++) curve.SmoothTangents(index, 0f);
            Curve(clip, path, "localEulerAnglesRaw.z", curve);
            Scale(clip, path, !rest && emotion == 2 && part != "Tail" ? .84f : 1f,
                !rest && emotion == 2 && part != "Tail" ? 1.03f : 1f);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static void Scale(AnimationClip clip, string path, float x, float y)
        {
            Curve(clip, path, "m_LocalScale.x", AnimationCurve.Constant(0, clip.length, x));
            Curve(clip, path, "m_LocalScale.y", AnimationCurve.Constant(0, clip.length, y));
            Curve(clip, path, "m_LocalScale.z", AnimationCurve.Constant(0, clip.length, 1f));
        }

        private static void Curve(AnimationClip clip, string path, string property, AnimationCurve curve) =>
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);

        [MenuItem("Work Game/Character/Verify Emotion Ear And Tail Motions")]
        public static void Verify()
        {
            CharacterExpressionSetup.Verify();
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var animator = root.GetComponent<Animator>();
                var controller = (AnimatorController)animator.runtimeAnimatorController;
                var head = root.transform.Find(Head);
                var pivots = Parts.Select(part => root.transform.Find(PathFor(part))).ToArray();
                var positions = pivots.Select(pivot => pivot.localPosition).ToArray();
                animator.Rebind();
                animator.Update(0f);
                for (var emotion = 0; emotion <= 3; emotion++)
                {
                    animator.SetInteger("Expression", emotion);
                    animator.Update(.02f);
                    animator.Update(.3f);
                    var ranges = new Vector2[3];
                    for (var index = 0; index < 3; index++) ranges[index] = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
                    for (var frame = 0; frame < 288; frame++)
                    {
                        animator.Update(1f / 60f);
                        for (var index = 0; index < 3; index++)
                        {
                            if (Vector3.Distance(pivots[index].localPosition, positions[index]) > .0001f)
                                throw new InvalidOperationException("Attachment pivot moved: " + Parts[index]);
                            var angle = Mathf.DeltaAngle(0f, pivots[index].localEulerAngles.z);
                            ranges[index].x = Mathf.Min(ranges[index].x, angle);
                            ranges[index].y = Mathf.Max(ranges[index].y, angle);
                        }
                        if ((emotion == 2 || emotion == 3) && Mathf.Abs(Mathf.DeltaAngle(0, head.localEulerAngles.z)) > .01f)
                            throw new InvalidOperationException("Angry/Happy head must stay steady.");
                    }
                    if (emotion == 1 && (ranges[0].y > -23.9f || ranges[1].y - ranges[1].x < 2f))
                        throw new InvalidOperationException("Cry needs a dropped tail and trembling ears.");
                    if (emotion == 2 && (Mathf.Abs(ranges[0].x - 22f) > .01f || ranges.Any(range => range.y - range.x > .01f)))
                        throw new InvalidOperationException("Angry ears and tail must hold their raised pose.");
                    if (emotion == 3 && (ranges[0].y - ranges[0].x < 10f || ranges[1].y - ranges[1].x < 6f))
                        throw new InvalidOperationException("Happy needs ear perks and stronger tail sway.");
                    if (emotion == 0 && pivots.Any(pivot => Vector3.Distance(pivot.localScale, Vector3.one) > .001f))
                        throw new InvalidOperationException("Normal scales were not restored.");
                    Capture(root, "Temp/CharacterEmotion-" + Emotions[emotion] + ".png");
                }
                animator.SetInteger("Expression", 0);
                animator.Update(.02f);
                animator.Update(.3f);
                if (pivots.Any(pivot => Vector3.Distance(pivot.localScale, Vector3.one) > .001f))
                    throw new InvalidOperationException("Angry flattened ear scales leaked into Normal.");
                File.WriteAllText("Temp/CharacterEmotionMotionVerification.txt", "PASS: 288 frames per expression; correct ear/tail motion ranges, fixed attachment pivots, Angry/Happy steady head, Normal scale restoration and all expression transitions.");
                Debug.Log("Character emotional ear/tail motion verification passed.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void Capture(GameObject root, string file)
        {
            var cameraObject = new GameObject("Character Preview Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, root.scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = root.scene;
            var renderers = root.GetComponentsInChildren<SpriteRenderer>().Where(renderer => renderer.enabled).ToArray();
            var savedMaterials = renderers.Select(renderer => renderer.sharedMaterials).ToArray();
            // The isolated preview scene has no 2D lights. Use an unlit material only for this capture.
            var previewShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            var previewMaterial = new Material(previewShader);
            foreach (var renderer in renderers) renderer.sharedMaterial = previewMaterial;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            camera.transform.position = bounds.center + Vector3.back * 20;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100;
            var target = RenderTexture.GetTemporary(640, 640, 24);
            var previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
                image.Apply();
                File.WriteAllBytes(file, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                for (var index = 0; index < renderers.Length; index++) renderers[index].sharedMaterials = savedMaterials[index];
                UnityEngine.Object.DestroyImmediate(previewMaterial);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
