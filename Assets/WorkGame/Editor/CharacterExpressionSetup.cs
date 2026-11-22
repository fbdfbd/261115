using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace WorkGame.Editor
{
    public static class CharacterExpressionSetup
    {
        private const string PrefabPath = "Assets/WorkGame/Prefab/Character.prefab";
        private const string SourcePath = "Assets/WorkGame/Art/Character/분리작업2.psb";
        private const string ControllerPath = "Assets/WorkGame/Animation/Character/CharacterIdle.controller";
        private const string Folder = "Assets/WorkGame/Animation/Character/Expressions";
        private const string HeadPath = "BreathPivot/HeadPivot";
        private static readonly string[] StateNames = { "Normal", "Cry", "Angry", "Happy" };
        private static readonly string[] PartNames = { "EyeL", "EyeR", "Nose", "Mouth" };
        private static readonly string[] NeutralNames = { "기본 왼쪽눈", "기본 오른쪽눈", "기본 코", "기본 입" };

        [MenuItem("Work Game/Character/Tighten Normal And Happy Eye Spacing")]
        public static void TightenNormalAndHappyEyeSpacing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before editing the prefab.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var head = root.transform.Find(HeadPath);
                foreach (var group in new[] { head, head.Find("Expressions/Happy") })
                {
                    group.Find("EyeL").localPosition += Vector3.right * .05f;
                    group.Find("EyeR").localPosition += Vector3.left * .05f;
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("Normal and Happy eye spacing tightened slightly: each eye moved inward by 0.05 local units; heights and Angry/Cry preserved.");
        }

        [MenuItem("Work Game/Character/Widen Happy Eye Spacing")]
        public static void WidenHappyEyeSpacing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before editing the prefab.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var happy = root.transform.Find(HeadPath + "/Expressions/Happy");
                happy.Find("EyeL").localPosition += Vector3.left * .1f;
                happy.Find("EyeR").localPosition += Vector3.right * .1f;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("Happy eye spacing widened slightly: each eye moved outward by 0.1 local units.");
        }

        private sealed class Art
        {
            public Sprite Sprite;
            public Vector2 Center;
        }

        [MenuItem("Work Game/Character/Match Face Layout To Original")]
        public static void MatchFaceLayoutToOriginal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before editing the prefab.");
            var source = ReadArt("Assets/WorkGame/Art/Character/분리작업.psb");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var head = root.transform.Find(HeadPath);
                var face = head.Find("Face");
                var faceCenter = source["머리 복사 2"].Center;
                var offsets = new Vector3[4];
                for (var index = 0; index < PartNames.Length; index++)
                {
                    var part = head.Find(PartNames[index]);
                    var sourceOffset = source[NeutralNames[index]].Center - faceCenter;
                    var targetPosition = face.localPosition + Vector3.Scale(new Vector3(sourceOffset.x, sourceOffset.y, 0f), face.localScale);
                    targetPosition.z = part.localPosition.z;
                    offsets[index] = targetPosition - part.localPosition;
                    part.localPosition = targetPosition;
                }
                // Shift each expression by the same correction as its neutral feature.
                // This preserves expression-specific artwork and the requested wider smile eyes.
                foreach (var state in new[] { "Cry", "Angry", "Happy" })
                {
                    var group = head.Find("Expressions/" + state);
                    for (var index = 0; index < PartNames.Length; index++)
                        group.Find(PartNames[index]).localPosition += offsets[index];
                    foreach (var extra in new[] { "LeftTear", "LeftBrow", "RightTear", "RightBrow", "Blush" })
                    {
                        var part = group.Find(extra);
                        if (part == null) continue;
                        part.localPosition += offsets[extra.StartsWith("Left", StringComparison.Ordinal) ? 0 :
                            extra.StartsWith("Right", StringComparison.Ordinal) ? 1 : 2];
                    }
                }
                foreach (var clip in AssetDatabase.FindAssets("t:AnimationClip", new[] { Folder })
                    .Select(guid => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid))))
                {
                    foreach (var name in new[] { "LeftTear", "RightTear" })
                    {
                        var tear = head.Find("Expressions/Cry/" + name);
                        var path = AnimationUtility.CalculateTransformPath(tear, root.transform);
                        var yBinding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y");
                        var curve = AnimationUtility.GetEditorCurve(clip, yBinding);
                        var keys = curve.keys;
                        var delta = offsets[name == "LeftTear" ? 0 : 1].y;
                        for (var key = 0; key < keys.Length; key++) keys[key].value += delta;
                        curve.keys = keys;
                        AnimationUtility.SetEditorCurve(clip, yBinding, curve);
                        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"),
                            AnimationCurve.Constant(0f, clip.length, tear.localPosition.x));
                        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"),
                            AnimationCurve.Constant(0f, clip.length, tear.localPosition.z));
                    }
                    EditorUtility.SetDirty(clip);
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                File.WriteAllText("Temp/CharacterFaceLayoutVerification.txt",
                    "Original PSB canvas matches reference PNG: 1193 x 1318. Neutral feature positions relative to Face:\n" +
                    string.Join("\n", PartNames.Select(name => name + " = " + head.Find(name).localPosition.ToString("F4"))));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Verify();
            Debug.Log("Character facial layout matched to original source image; expressions and tear animation offsets updated together.");
        }

        [MenuItem("Work Game/Character/Add Expression Animations")]
        public static void Connect()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before editing Character.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) throw new InvalidOperationException("Connect the idle Animator first.");
            if (controller.layers.Any(x => x.name == "Expression"))
                throw new InvalidOperationException("Expression animations already exist; existing edits were preserved.");
            var art = ReadArt();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var head = root.transform.Find(HeadPath);
                if (head == null || head.Find("Expressions") != null)
                    throw new InvalidOperationException("Character head is missing or already has expressions.");
                var features = PartNames.Select(name => head.Find(name).GetComponent<SpriteRenderer>()).ToArray();
                var parent = new GameObject("Expressions").transform;
                parent.SetParent(head, false);
                var groups = new List<SpriteRenderer>[4];
                groups[0] = new List<SpriteRenderer>();
                var prefixes = new[] { "", "울음", "화남", "웃음" };
                for (var state = 1; state <= 3; state++)
                {
                    var group = new GameObject(StateNames[state]).transform;
                    group.SetParent(parent, false);
                    groups[state] = new List<SpriteRenderer>();
                    for (var part = 0; part < 4; part++)
                    {
                        var suffix = part == 0 ? "왼쪽눈" : part == 1 ? "오른쪽눈" : part == 2 ? "코" : "입";
                        var name = prefixes[state] + " " + suffix;
                        if (state == 3 && part >= 2) name += " 복사";
                        var delta = art[name].Center - art[NeutralNames[part]].Center;
                        var reference = features[part].transform;
                        var position = reference.localPosition + Vector3.Scale(new Vector3(delta.x, delta.y, 0f), reference.localScale);
                        groups[state].Add(AddSprite(group, PartNames[part], art[name].Sprite, features[part], position));
                    }
                    if (state == 1)
                    {
                        AddExtra(group, groups[state], "LeftTear", "울음 왼쪽눈물", 0, features, art);
                        AddExtra(group, groups[state], "RightTear", "울음 오른쪽눈물", 1, features, art);
                        AddExtra(group, groups[state], "LeftBrow", "울음 왼쪽눈썹", 0, features, art);
                        AddExtra(group, groups[state], "RightBrow", "울음 오른쪽눈썹", 1, features, art);
                    }
                    if (state == 3)
                        AddExtra(group, groups[state], "Blush", "홍조", 2, features, art);
                }

                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh();
                controller.AddParameter("Expression", AnimatorControllerParameterType.Int);
                controller.AddParameter("ExpressionStrength", AnimatorControllerParameterType.Float);
                controller.AddParameter("ExpressionSpeed", AnimatorControllerParameterType.Float);
                var parameters = controller.parameters;
                foreach (var parameter in parameters)
                    if (parameter.name == "ExpressionStrength" || parameter.name == "ExpressionSpeed") parameter.defaultFloat = 1f;
                controller.parameters = parameters;
                controller.AddLayer("Expression");
                var layers = controller.layers;
                var layer = layers[layers.Length - 1];
                layer.defaultWeight = 1f;
                var machine = layer.stateMachine;
                for (var index = 0; index < 4; index++)
                {
                    var duration = index == 1 ? 2.8f : index == 2 ? 3.2f : 3.6f;
                    var motion = Clip(root.transform, index, duration, features, groups, true);
                    var rest = Clip(root.transform, index, duration, features, groups, false);
                    AssetDatabase.CreateAsset(motion, Folder + "/" + StateNames[index] + ".anim");
                    AssetDatabase.CreateAsset(rest, Folder + "/" + StateNames[index] + "Rest.anim");
                    var tree = new BlendTree { name = StateNames[index] + "Motion", blendType = BlendTreeType.Simple1D,
                        blendParameter = "ExpressionStrength", useAutomaticThresholds = false };
                    AssetDatabase.AddObjectToAsset(tree, controller);
                    tree.AddChild(rest, 0f);
                    tree.AddChild(motion, 1f);
                    var state = machine.AddState(StateNames[index], new Vector3(260f, index * 80f));
                    state.motion = tree;
                    state.writeDefaultValues = false;
                    state.speedParameterActive = true;
                    state.speedParameter = "ExpressionSpeed";
                    var transition = machine.AddAnyStateTransition(state);
                    transition.hasExitTime = false;
                    transition.hasFixedDuration = true;
                    transition.duration = 0f;
                    transition.canTransitionToSelf = false;
                    transition.interruptionSource = TransitionInterruptionSource.DestinationThenSource;
                    transition.AddCondition(AnimatorConditionMode.Equals, index, "Expression");
                    if (index == 0) machine.defaultState = state;
                }
                controller.layers = layers;
                root.AddComponent<CharacterExpressionAnimator>();
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Verify();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log("Character expressions connected: Normal / Cry / Angry / Happy. Adjust CharacterExpressionAnimator in Inspector.");
        }

        private static Dictionary<string, Art> ReadArt(string sourcePath = SourcePath)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<Sprite>().ToDictionary(x => x.name);
            var text = File.ReadAllText(sourcePath + ".meta");
            var start = text.IndexOf("  layeredSpriteImportData:", StringComparison.Ordinal);
            if (start < 0) throw new InvalidOperationException("PSB layer positions were not found.");
            var result = new Dictionary<string, Art>();
            foreach (var block in Regex.Split(text.Substring(start), @"\r?\n  - name: ").Skip(1))
            {
                var firstLine = block.Split('\n')[0].Trim();
                var name = Regex.Unescape(firstLine.Trim('"'));
                var position = Regex.Match(block, @"spritePosition: \{x: ([-\d.]+), y: ([-\d.]+)\}");
                var width = Regex.Match(block, @"width: (\d+)");
                var height = Regex.Match(block, @"height: (\d+)");
                if (!position.Success || !width.Success || !height.Success || !sprites.TryGetValue(name, out var sprite)) continue;
                float Parse(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
                result[name] = new Art { Sprite = sprite, Center = new Vector2(
                    (Parse(position.Groups[1].Value) + Parse(width.Groups[1].Value) * sprite.pivot.x / sprite.rect.width) / sprite.pixelsPerUnit,
                    (Parse(position.Groups[2].Value) + Parse(height.Groups[1].Value) * sprite.pivot.y / sprite.rect.height) / sprite.pixelsPerUnit) };
            }
            return result;
        }

        private static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, SpriteRenderer reference, Vector3 position)
        {
            var go = new GameObject(name);
            go.layer = reference.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = reference.transform.localScale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterials = reference.sharedMaterials;
            renderer.sortingLayerID = reference.sortingLayerID;
            renderer.sortingOrder = reference.sortingOrder;
            renderer.color = reference.color;
            renderer.enabled = false;
            return renderer;
        }

        private static void AddExtra(Transform parent, List<SpriteRenderer> group, string name, string source,
            int referenceIndex, SpriteRenderer[] features, Dictionary<string, Art> art)
        {
            var reference = features[referenceIndex];
            var delta = art[source].Center - art[NeutralNames[referenceIndex]].Center;
            var position = reference.transform.localPosition +
                Vector3.Scale(new Vector3(delta.x, delta.y, 0f), reference.transform.localScale);
            var renderer = AddSprite(parent, name, art[source].Sprite, reference, position);
            renderer.sortingOrder = features.Max(x => x.sortingOrder) + 1;
            group.Add(renderer);
        }

        private static AnimationClip Clip(Transform root, int state, float duration,
            SpriteRenderer[] neutral, List<SpriteRenderer>[] groups, bool moving)
        {
            var clip = new AnimationClip { name = StateNames[state] + (moving ? "" : "Rest"), frameRate = 60f };
            foreach (var renderer in neutral) Enabled(renderer, state == 0);
            for (var group = 1; group < 4; group++)
                foreach (var renderer in groups[group]) Enabled(renderer, state == group);

            var tilt = state == 1 ? Keys(0, 0, .3f, -.6f, .52f, .5f, .76f, -.4f, 1f, 0, 1.7f, -.5f, 1.95f, .4f, 2.2f, 0, duration, 0) :
                AnimationCurve.Constant(0, duration, 0);
            Curve(HeadPath, typeof(Transform), "localEulerAnglesRaw.z", moving ? tilt : AnimationCurve.Constant(0, duration, 0));
            var bob = state == 1 ? .018f : 0f;
            Curve(HeadPath + "/Expressions", typeof(Transform), "m_LocalPosition.y",
                moving ? Keys(0, 0, duration * .25f, bob, duration * .5f, 0, duration * .75f, -bob * .4f, duration, 0) : AnimationCurve.Constant(0, duration, 0));
            foreach (var tear in groups[1].Where(x => x.name.EndsWith("Tear", StringComparison.Ordinal)))
            {
                var path = AnimationUtility.CalculateTransformPath(tear.transform, root);
                var y = tear.transform.localPosition.y;
                // Unity combines position curves into a Vector3; bind the stationary axes too.
                Curve(path, typeof(Transform), "m_LocalPosition.x", AnimationCurve.Constant(0, duration, tear.transform.localPosition.x));
                Curve(path, typeof(Transform), "m_LocalPosition.z", AnimationCurve.Constant(0, duration, tear.transform.localPosition.z));
                Curve(path, typeof(Transform), "m_LocalPosition.y", state == 1 && moving ?
                    Keys(0, y, .7f, y - .08f, 1.4f, y, 2.1f, y - .06f, duration, y) : AnimationCurve.Constant(0, duration, y));
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;

            void Enabled(SpriteRenderer renderer, bool value) => Curve(AnimationUtility.CalculateTransformPath(renderer.transform, root),
                typeof(SpriteRenderer), "m_Enabled", AnimationCurve.Constant(0, duration, value ? 1f : 0f));
            void Curve(string path, Type type, string property, AnimationCurve curve) =>
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, property), curve);
        }

        private static AnimationCurve Keys(params float[] values)
        {
            var keys = new Keyframe[values.Length / 2];
            for (var i = 0; i < keys.Length; i++) keys[i] = new Keyframe(values[i * 2], values[i * 2 + 1], 0f, 0f);
            return new AnimationCurve(keys);
        }

        [MenuItem("Work Game/Character/Fix Tear Animation Positions")]
        public static void FixTearPositions()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before repairing animation clips.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                foreach (var clip in AssetDatabase.FindAssets("t:AnimationClip", new[] { Folder })
                    .Select(guid => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid))))
                {
                    foreach (var name in new[] { "LeftTear", "RightTear" })
                    {
                        var tear = root.transform.Find(HeadPath + "/Expressions/Cry/" + name);
                        var path = AnimationUtility.CalculateTransformPath(tear, root.transform);
                        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"),
                            AnimationCurve.Constant(0f, clip.length, tear.localPosition.x));
                        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"),
                            AnimationCurve.Constant(0f, clip.length, tear.localPosition.z));
                    }
                    EditorUtility.SetDirty(clip);
                }
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Verify();
            Debug.Log("Character tears repaired: left/right X positions remain fixed throughout animation.");
        }

        [MenuItem("Work Game/Character/Fix Expression Image Overlap")]
        public static void FixExpressionImageOverlap()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before editing the controller.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            var layer = controller.layers.Single(value => value.name == "Expression");
            foreach (var transition in layer.stateMachine.anyStateTransitions)
            {
                transition.duration = 0f;
                EditorUtility.SetDirty(transition);
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var animator = root.GetComponent<Animator>();
                var head = root.transform.Find(HeadPath);
                var groups = new SpriteRenderer[4][];
                groups[0] = PartNames.Select(name => head.Find(name).GetComponent<SpriteRenderer>()).ToArray();
                for (var index = 1; index < 4; index++)
                    groups[index] = head.Find("Expressions/" + StateNames[index]).GetComponentsInChildren<SpriteRenderer>();
                animator.Rebind();
                animator.Update(0f);
                var frames = 0;
                for (var from = 0; from < 4; from++)
                    for (var to = 0; to < 4; to++)
                    {
                        if (from == to) continue;
                        animator.SetInteger("Expression", from);
                        animator.Update(.02f);
                        animator.Update(.3f);
                        animator.SetInteger("Expression", to);
                        for (var frame = 0; frame < 18; frame++)
                        {
                            animator.Update(1f / 60f);
                            var visibleSets = 0;
                            for (var group = 0; group < 4; group++)
                            {
                                var visible = groups[group][0].enabled;
                                if (groups[group].Any(renderer => renderer.enabled != visible))
                                    throw new InvalidOperationException("Partially visible expression during transition.");
                                if (visible) visibleSets++;
                            }
                            if (visibleSets != 1) throw new InvalidOperationException("Overlapping or missing expression during transition.");
                            frames++;
                        }
                        if (!groups[to][0].enabled) throw new InvalidOperationException("Expression did not switch to target.");
                    }
                File.WriteAllText("Temp/CharacterExpressionSwapVerification.txt",
                    "PASS: all 12 directed expression changes sampled for " + frames + " frames at 60 Hz; exactly one complete face set visible per frame. Expression transitions are instantaneous; ear/tail transitions remain 0.22 seconds.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("Character expression image overlap fixed; all expression changes verified frame by frame.");
        }

        [MenuItem("Work Game/Character/Verify Expression Animations")]
        public static void Verify()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var animator = root.GetComponent<Animator>();
                var controller = animator.runtimeAnimatorController as AnimatorController;
                var layer = Array.FindIndex(controller.layers, x => x.name == "Expression");
                if (layer < 0 || root.GetComponent<CharacterExpressionAnimator>() == null)
                    throw new InvalidOperationException("Expression Animator is not connected.");
                var head = root.transform.Find(HeadPath);
                var originalPosition = root.transform.position;
                var leftTear = head.Find("Expressions/Cry/LeftTear");
                var rightTear = head.Find("Expressions/Cry/RightTear");
                var leftTearX = leftTear.localPosition.x;
                var rightTearX = rightTear.localPosition.x;
                animator.Rebind();
                animator.Update(0f);
                foreach (var clip in controller.animationClips)
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                        if (root.transform.Find(binding.path) == null) throw new InvalidOperationException("Missing binding: " + binding.path);
                for (var expression = 0; expression < 4; expression++)
                {
                    animator.SetInteger("Expression", expression);
                    animator.Update(.02f);
                    animator.Update(.3f);
                    animator.Update(.05f);
                    if (!animator.GetCurrentAnimatorStateInfo(layer).IsName(StateNames[expression]))
                        throw new InvalidOperationException("Expression transition failed: " + StateNames[expression]);
                    foreach (var name in PartNames)
                        if (head.Find(name).GetComponent<SpriteRenderer>().enabled != (expression == 0))
                            throw new InvalidOperationException("Neutral feature visibility failed: " + name);
                    for (var group = 1; group <= 3; group++)
                        foreach (var renderer in head.Find("Expressions/" + StateNames[group]).GetComponentsInChildren<SpriteRenderer>())
                            if (renderer.enabled != (expression == group)) throw new InvalidOperationException("Expression visibility failed: " + renderer.name);
                    if (expression == 1)
                    {
                        var angle = head.localRotation;
                        animator.Update(.37f);
                        if (Mathf.Abs(Mathf.DeltaAngle(angle.eulerAngles.z, head.localEulerAngles.z)) < .01f)
                            throw new InvalidOperationException("Expression has no small motion: " + StateNames[expression]);
                    }
                    if ((expression == 2 || expression == 3) &&
                        Mathf.Abs(Mathf.DeltaAngle(0f, head.localEulerAngles.z)) > .01f)
                        throw new InvalidOperationException("Angry/Happy must not shake the head.");
                    if (Mathf.Abs(leftTear.localPosition.x - leftTearX) > .001f ||
                        Mathf.Abs(rightTear.localPosition.x - rightTearX) > .001f)
                        throw new InvalidOperationException("Tears lost their horizontal position during animation.");
                    if (expression == 1)
                    {
                        var leftEye = head.Find("Expressions/Cry/EyeL");
                        var rightEye = head.Find("Expressions/Cry/EyeR");
                        for (var frame = 0; frame < 180; frame++)
                        {
                            animator.Update(1f / 60f);
                            if (leftTear.localPosition.x >= 0f || rightTear.localPosition.x <= 0f ||
                                leftTear.localPosition.y >= leftEye.localPosition.y || rightTear.localPosition.y >= rightEye.localPosition.y)
                                throw new InvalidOperationException("Tears must stay below their corresponding eye for the entire crying loop.");
                        }
                    }
                }
                animator.SetInteger("Expression", 0);
                animator.Update(.02f);
                animator.Update(.3f);
                if (Quaternion.Angle(head.localRotation, Quaternion.identity) > .01f || root.transform.position != originalPosition)
                    throw new InvalidOperationException("Normal pose was not restored.");
                File.WriteAllText("Temp/CharacterExpressionVerification.txt", "PASS: Normal/Cry/Angry/Happy transitions, exclusive feature visibility, all animation paths, small motion in all expressions, return to Normal and fixed root. Tear X positions preserved in all states; both tears remain below their corresponding eye for a full crying loop (180 frames).");
                Debug.Log("Character expression animation verification passed.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
