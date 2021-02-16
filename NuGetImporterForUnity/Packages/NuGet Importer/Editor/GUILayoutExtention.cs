using UnityEditor;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Additional GUILayout</para>
    /// <para>追加(カスタム)のGUILayout</para>
    /// </summary>
    public static class GUILayoutExtention
    {
        /// <summary>
        /// <para>Label for URL.</para>
        /// <para>URLのためのラベル。</para>
        /// </summary>
        /// <param name="text">
        /// <para>Shown text.</para>
        /// <para>表示されるテキスト。</para>
        /// </param>
        /// <param name="url">
        /// <para>URL to go to when clicked.</para>
        /// <para>クリックしたときに飛ぶURL。</para>
        /// </param>
        /// <param name="fontSize">
        /// <para>Text font size. If less than 0, this become default. (option)</para>
        /// <para>テキストのフォントサイズ。0以下の場合デフォルト値になる。（オプション）</para>
        /// </param>
        /// <param name="style">
        /// <para>The style to use. (option)</para>
        /// <para>使用される<c>GUIStyle</c>。（オプション）</para>
        /// </param>
        /// <param name="options">
        /// <para>An optional list of layout options that specify extra layouting properties.</para>
        /// <para>追加のレイアウトプロパティを指定するレイアウトオプションのオプションリスト。</para>
        /// </param>
        public static void UrlLabel(string text, string url, int fontSize = -1, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (style == null)
            {
                style = new GUIStyle(EditorStyles.label);
            }
            if (fontSize < 0)
            {
                fontSize = GUI.skin.label.fontSize;
            }
            GUIStyleState styleState = style.normal;
            styleState.textColor = Color.blue;
            style.normal = styleState;
            style.fontSize = fontSize;

            GUILayout.Label(text, style, options);
            Rect rect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);
            Event nowEvent = Event.current;
            switch (nowEvent.type)
            {
                case EventType.MouseDown:
                    if (rect.Contains(nowEvent.mousePosition))
                    {
                        Help.BrowseURL(url);
                    }
                    break;
            }
        }

        /// <summary>
        /// <para>Labels to be automatically wrapped.</para>
        /// <para>自動で改行できるラベル。</para>
        /// </summary>
        /// <param name="text">
        /// <para>Shown text.</para>
        /// <para>表示されるテキスト。</para>
        /// </param>
        /// <param name="fontSize">
        /// <para>Text font size. If less than 0, this become default. (option)</para>
        /// <para>テキストのフォントサイズ。0以下の場合デフォルト値になる。（オプション）</para>
        /// </param>
        /// <param name="style">
        /// <para>The style to use. (option)</para>
        /// <para>使用される<c>GUIStyle</c>。（オプション）</para>
        /// </param>
        /// <param name="options">
        /// <para>An optional list of layout options that specify extra layouting properties.</para>
        /// <para>追加のレイアウトプロパティを指定するレイアウトオプションのオプションリスト。</para>
        /// </param>
        public static void WrapedLabel(string text, int fontSize = -1, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (style == null)
            {
                style = new GUIStyle(EditorStyles.label);
            }

            if (fontSize < 0)
            {
                fontSize = GUI.skin.label.fontSize;
            }
            style.wordWrap = true;
            style.fontSize = fontSize;

            GUILayout.Label(text, style, options);
        }
    }
}
