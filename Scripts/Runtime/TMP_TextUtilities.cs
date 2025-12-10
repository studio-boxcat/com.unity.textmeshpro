using UnityEngine;
using System.Collections;


namespace TMPro
{
    public enum CaretPosition { None, Left, Right }

    /// <summary>
    /// Structure which contains the character index and position of caret relative to the character.
    /// </summary>
    public struct CaretInfo
    {
        public int index;
        public CaretPosition position;

        public CaretInfo(int index, CaretPosition position)
        {
            this.index = index;
            this.position = position;
        }
    }

    public static class TMP_TextUtilities
    {
        private static Vector3[] m_rectWorldCorners = new Vector3[4];


        // TEXT INPUT COMPONENT RELATED FUNCTIONS

        /// <summary>
        ///
        /// </summary>
        /// <param name="textComponent">A reference to the text object.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>
        //public static CaretInfo GetCursorInsertionIndex(TMP_Text textComponent, Vector3 position, Camera camera)
        //{
        //    int index = TMP_TextUtilities.FindNearestCharacter(textComponent, position, camera, false);

        //    RectTransform rectTransform = textComponent.rectTransform;

        //    // Convert position into Worldspace coordinates
        //    ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

        //    TMP_CharacterInfo cInfo = textComponent.textInfo.characterInfo[index];

        //    // Get Bottom Left and Top Right position of the current character
        //    Vector3 bl = rectTransform.TransformPoint(cInfo.bottomLeft);
        //    //Vector3 tl = rectTransform.TransformPoint(new Vector3(cInfo.bottomLeft.x, cInfo.topRight.y, 0));
        //    Vector3 tr = rectTransform.TransformPoint(cInfo.topRight);
        //    //Vector3 br = rectTransform.TransformPoint(new Vector3(cInfo.topRight.x, cInfo.bottomLeft.y, 0));

        //    float insertPosition = (position.x - bl.x) / (tr.x - bl.x);

        //    if (insertPosition < 0.5f)
        //        return new CaretInfo(index, CaretPosition.Left);
        //    else
        //        return new CaretInfo(index, CaretPosition.Right);
        //}


        /// <summary>
        /// Function returning the index of the character whose origin is closest to the cursor.
        /// </summary>
        /// <param name="textComponent">A reference to the text object.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>
        public static int GetCursorIndexFromPosition(TMP_Text textComponent, Vector3 position, Camera camera)
        {
            int index = TMP_TextUtilities.FindNearestCharacter(textComponent, position, camera, false);

            RectTransform rectTransform = textComponent.rectTransform;

            // Convert position into Worldspace coordinates
            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            TMP_CharacterInfo cInfo = textComponent.textInfo.characterInfo[index];

            // Get Bottom Left and Top Right position of the current character
            Vector3 bl = rectTransform.TransformPoint(cInfo.bottomLeft);
            Vector3 tr = rectTransform.TransformPoint(cInfo.topRight);

            float insertPosition = (position.x - bl.x) / (tr.x - bl.x);

            if (insertPosition < 0.5f)
                return index;
            else
                return index + 1;

        }


        /// <summary>
        /// Function returning the index of the character whose origin is closest to the cursor.
        /// </summary>
        /// <param name="textComponent">A reference to the text object.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <param name="cursor">The position of the cursor insertion position relative to the position.</param>
        /// <returns></returns>
        //public static int GetCursorIndexFromPosition(TMP_Text textComponent, Vector3 position, Camera camera, out CaretPosition cursor)
        //{
        //    int index = TMP_TextUtilities.FindNearestCharacter(textComponent, position, camera, false);

        //    RectTransform rectTransform = textComponent.rectTransform;

        //    // Convert position into Worldspace coordinates
        //    ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

        //    TMP_CharacterInfo cInfo = textComponent.textInfo.characterInfo[index];

        //    // Get Bottom Left and Top Right position of the current character
        //    Vector3 bl = rectTransform.TransformPoint(cInfo.bottomLeft);
        //    Vector3 tr = rectTransform.TransformPoint(cInfo.topRight);

        //    float insertPosition = (position.x - bl.x) / (tr.x - bl.x);

        //    if (insertPosition < 0.5f)
        //    {
        //        cursor = CaretPosition.Left;
        //        return index;
        //    }
        //    else
        //    {
        //        cursor = CaretPosition.Right;
        //        return index;
        //    }
        //}


        /// <summary>
        /// Function returning the index of the character whose origin is closest to the cursor.
        /// </summary>
        /// <param name="textComponent">A reference to the text object.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <param name="cursor">The position of the cursor insertion position relative to the position.</param>
        /// <returns></returns>
        public static int GetCursorIndexFromPosition(TMP_Text textComponent, Vector3 position, Camera camera, out CaretPosition cursor)
        {
            int line = TMP_TextUtilities.FindNearestLine(textComponent, position, camera);

            int index = FindNearestCharacterOnLine(textComponent, position, line, camera, false);

            // Special handling if line contains only one character.
            if (textComponent.textInfo.lineInfo[line].characterCount == 1)
            {
                cursor = CaretPosition.Left;
                return index;
            }

            RectTransform rectTransform = textComponent.rectTransform;

            // Convert position into Worldspace coordinates
            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            TMP_CharacterInfo cInfo = textComponent.textInfo.characterInfo[index];

            // Get Bottom Left and Top Right position of the current character
            Vector3 bl = rectTransform.TransformPoint(cInfo.bottomLeft);
            Vector3 tr = rectTransform.TransformPoint(cInfo.topRight);

            float insertPosition = (position.x - bl.x) / (tr.x - bl.x);

            if (insertPosition < 0.5f)
            {
                cursor = CaretPosition.Left;
                return index;
            }
            else
            {
                cursor = CaretPosition.Right;
                return index;
            }
        }


        /// <summary>
        /// Function returning the line nearest to the position.
        /// </summary>
        /// <param name="textComponent"></param>
        /// <param name="position"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static int FindNearestLine(TMP_Text text, Vector3 position, Camera camera)
        {
            RectTransform rectTransform = text.rectTransform;

            float distance = Mathf.Infinity;
            int closest = -1;

            // Convert position into Worldspace coordinates
            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            for (int i = 0; i < text.textInfo.lineCount; i++)
            {
                TMP_LineInfo lineInfo = text.textInfo.lineInfo[i];

                float ascender = rectTransform.TransformPoint(new Vector3(0, lineInfo.ascender, 0)).y;
                float descender = rectTransform.TransformPoint(new Vector3(0, lineInfo.descender, 0)).y;

                if (ascender > position.y && descender < position.y)
                {
                    //Debug.Log("Position is on line " + i);
                    return i;
                }

                float d0 = Mathf.Abs(ascender - position.y);
                float d1 = Mathf.Abs(descender - position.y);

                float d = Mathf.Min(d0, d1);
                if (d < distance)
                {
                    distance = d;
                    closest = i;
                }
            }

            //Debug.Log("Closest line to position is " + closest);
            return closest;
        }


        /// <summary>
        /// Function returning the nearest character to position on a given line.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="position"></param>
        /// <param name="line"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static int FindNearestCharacterOnLine(TMP_Text text, Vector3 position, int line, Camera camera, bool visibleOnly)
        {
            RectTransform rectTransform = text.rectTransform;

            // Convert position into Worldspace coordinates
            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            int firstCharacter = text.textInfo.lineInfo[line].firstCharacterIndex;
            int lastCharacter = text.textInfo.lineInfo[line].lastCharacterIndex;

            float distanceSqr = Mathf.Infinity;
            int closest = lastCharacter;

            for (int i = firstCharacter; i < lastCharacter; i++)
            {
                // Get current character info.
                TMP_CharacterInfo cInfo = text.textInfo.characterInfo[i];
                if (visibleOnly && !cInfo.isVisible) continue;

                // Get Bottom Left and Top Right position of the current character
                Vector3 bl = rectTransform.TransformPoint(cInfo.bottomLeft);
                Vector3 tl = rectTransform.TransformPoint(new Vector3(cInfo.bottomLeft.x, cInfo.topRight.y, 0));
                Vector3 tr = rectTransform.TransformPoint(cInfo.topRight);
                Vector3 br = rectTransform.TransformPoint(new Vector3(cInfo.topRight.x, cInfo.bottomLeft.y, 0));

                if (PointIntersectRectangle(position, bl, tl, tr, br))
                {
                    closest = i;
                    break;
                }

                // Find the closest corner to position.
                float dbl = DistanceToLine(bl, tl, position);
                float dtl = DistanceToLine(tl, tr, position);
                float dtr = DistanceToLine(tr, br, position);
                float dbr = DistanceToLine(br, bl, position);

                float d = dbl < dtl ? dbl : dtl;
                d = d < dtr ? d : dtr;
                d = d < dbr ? d : dbr;

                if (distanceSqr > d)
                {
                    distanceSqr = d;
                    closest = i;
                }
            }
            return closest;
        }


        /// <summary>
        /// Function used to determine if the position intersects with the RectTransform.
        /// </summary>
        /// <param name="rectTransform">A reference to the RectTranform of the text object.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>
        public static bool IsIntersectingRectTransform(RectTransform rectTransform, Vector3 position, Camera camera)
        {
            // Convert position into Worldspace coordinates
            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            rectTransform.GetWorldCorners(m_rectWorldCorners);

            if (PointIntersectRectangle(position, m_rectWorldCorners[0], m_rectWorldCorners[1], m_rectWorldCorners[2], m_rectWorldCorners[3]))
            {
                return true;
            }

            return false;
        }


        // CHARACTER HANDLING

        /// <summary>
        /// Function returning the index of the character at the given position (if any).
        /// </summary>
        /// <param name="text">A reference to the TextMeshPro component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which is rendering the text or whichever one might be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <param name="visibleOnly">Only check for visible characters.</param>
        /// <returns></returns>
        public static int FindIntersectingCharacter(TMP_Text text, Vector3 position, Camera camera, bool visibleOnly)
        {
            RectTransform rectTransform = text.rectTransform;

            // Convert position into Worldspace coordinates
            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            for (int i = 0; i < text.textInfo.characterCount; i++)
            {
                // Get current character info.
                TMP_CharacterInfo cInfo = text.textInfo.characterInfo[i];
                if (visibleOnly && !cInfo.isVisible) continue;

                // Get Bottom Left and Top Right position of the current character
                Vector3 bl = rectTransform.TransformPoint(cInfo.bottomLeft);
                Vector3 tl = rectTransform.TransformPoint(new Vector3(cInfo.bottomLeft.x, cInfo.topRight.y, 0));
                Vector3 tr = rectTransform.TransformPoint(cInfo.topRight);
                Vector3 br = rectTransform.TransformPoint(new Vector3(cInfo.topRight.x, cInfo.bottomLeft.y, 0));

                if (PointIntersectRectangle(position, bl, tl, tr, br))
                    return i;

            }
            return -1;
        }


        /// <summary>
        /// Function returning the index of the character at the given position (if any).
        /// </summary>
        /// <param name="text">A reference to the TextMeshPro UGUI component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The camera which is rendering the text object.</param>
        /// <param name="visibleOnly">Only check for visible characters.</param>
        /// <returns></returns>
        //public static int FindIntersectingCharacter(TextMeshPro text, Vector3 position, Camera camera, bool visibleOnly)
        //{
        //    Transform textTransform = text.transform;

        //    // Convert position into Worldspace coordinates
        //    ScreenPointToWorldPointInRectangle(textTransform, position, camera, out position);

        //    for (int i = 0; i < text.textInfo.characterCount; i++)
        //    {
        //        // Get current character info.
        //        TMP_CharacterInfo cInfo = text.textInfo.characterInfo[i];
        //        if ((visibleOnly && !cInfo.isVisible) || (text.OverflowMode == TextOverflowModes.Page && cInfo.pageNumber + 1 != text.pageToDisplay))
        //            continue;

        //        // Get Bottom Left and Top Right position of the current character
        //        Vector3 bl = textTransform.TransformPoint(cInfo.bottomLeft);
        //        Vector3 tl = textTransform.TransformPoint(new Vector3(cInfo.bottomLeft.x, cInfo.topRight.y, 0));
        //        Vector3 tr = textTransform.TransformPoint(cInfo.topRight);
        //        Vector3 br = textTransform.TransformPoint(new Vector3(cInfo.topRight.x, cInfo.bottomLeft.y, 0));

        //        if (PointIntersectRectangle(position, bl, tl, tr, br))
        //            return i;

        //    }

        //    return -1;
        //}


        /// <summary>
        /// Function to find the nearest character to position.
        /// </summary>
        /// <param name="text">A reference to the TMP Text component.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <param name="visibleOnly">Only check for visible characters.</param>
        /// <returns></returns>
        public static int FindNearestCharacter(TMP_Text text, Vector3 position, Camera camera, bool visibleOnly)
        {
            RectTransform rectTransform = text.rectTransform;

            float distanceSqr = Mathf.Infinity;
            int closest = 0;

            // Convert position into Worldspace coordinates
            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            for (int i = 0; i < text.textInfo.characterCount; i++)
            {
                // Get current character info.
                TMP_CharacterInfo cInfo = text.textInfo.characterInfo[i];
                if (visibleOnly && !cInfo.isVisible) continue;

                // Get Bottom Left and Top Right position of the current character
                Vector3 bl = rectTransform.TransformPoint(cInfo.bottomLeft);
                Vector3 tl = rectTransform.TransformPoint(new Vector3(cInfo.bottomLeft.x, cInfo.topRight.y, 0));
                Vector3 tr = rectTransform.TransformPoint(cInfo.topRight);
                Vector3 br = rectTransform.TransformPoint(new Vector3(cInfo.topRight.x, cInfo.bottomLeft.y, 0));

                if (PointIntersectRectangle(position, bl, tl, tr, br))
                    return i;

                // Find the closest corner to position.
                float dbl = DistanceToLine(bl, tl, position);
                float dtl = DistanceToLine(tl, tr, position);
                float dtr = DistanceToLine(tr, br, position);
                float dbr = DistanceToLine(br, bl, position);

                float d = dbl < dtl ? dbl : dtl;
                d = d < dtr ? d : dtr;
                d = d < dbr ? d : dbr;

                if (distanceSqr > d)
                {
                    distanceSqr = d;
                    closest = i;
                }
            }

            return closest;
        }

        /// <summary>
        /// Function to check if a Point is contained within a Rectangle.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        private static bool PointIntersectRectangle(Vector3 m, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 ab = b - a;
            Vector3 am = m - a;
            Vector3 bc = c - b;
            Vector3 bm = m - b;

            float abamDot = Vector3.Dot(ab, am);
            float bcbmDot = Vector3.Dot(bc, bm);

            return 0 <= abamDot && abamDot <= Vector3.Dot(ab, ab) && 0 <= bcbmDot && bcbmDot <= Vector3.Dot(bc, bc);
        }


        /// <summary>
        /// Method to convert ScreenPoint to WorldPoint aligned with Rectangle
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="screenPoint"></param>
        /// <param name="cam"></param>
        /// <param name="worldPoint"></param>
        /// <returns></returns>
        public static bool ScreenPointToWorldPointInRectangle(Transform transform, Vector2 screenPoint, Camera cam, out Vector3 worldPoint)
        {
            worldPoint = (Vector3)Vector2.zero;
            Ray ray = RectTransformUtility.ScreenPointToRay(cam, screenPoint);

            float enter;
            if (!new Plane(transform.rotation * Vector3.back, transform.position).Raycast(ray, out enter))
                return false;

            worldPoint = ray.GetPoint(enter);

            return true;
        }


        /// <summary>
        /// Function returning the Square Distance from a Point to a Line.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static float DistanceToLine(Vector3 a, Vector3 b, Vector3 point)
        {
            Vector3 n = b - a;
            Vector3 pa = a - point;

            float c = Vector3.Dot( n, pa );

            // Closest point is a
            if ( c > 0.0f )
                return Vector3.Dot( pa, pa );

            Vector3 bp = point - b;

            // Closest point is b
            if (Vector3.Dot( n, bp ) > 0.0f )
                return Vector3.Dot( bp, bp );

            // Closest point is between a and b
            Vector3 e = pa - n * (c / Vector3.Dot( n, n ));

            return Vector3.Dot( e, e );
        }


        /// <summary>
        /// Table used to convert character to uppercase.
        /// </summary>
        const string k_lookupStringU = "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[-]^_`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~-";

        /// <summary>
        /// Get uppercase version of this ASCII character.
        /// </summary>
        internal static uint ToUpperASCIIFast(uint c)
        {
            if (c > k_lookupStringU.Length - 1)
                return c;

            return k_lookupStringU[(int)c];
        }

        /// <summary>
        /// Function which returns a simple hashcode from a string.
        /// </summary>
        /// <returns></returns>
        public static int GetSimpleHashCode(string s)
        {
            int hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = ((hashCode << 5) + hashCode) ^ s[i];

            return hashCode;
        }

        /// <summary>
        /// Function to convert Hex to Int
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static int HexToInt(char hex)
        {
            switch (hex)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
                case 'a': return 10;
                case 'b': return 11;
                case 'c': return 12;
                case 'd': return 13;
                case 'e': return 14;
                case 'f': return 15;
            }
            return 15;
        }


        /// <summary>
        /// Function to convert a properly formatted string which contains an hex value to its decimal value.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int StringHexToInt(string s)
        {
            int value = 0;

            for (int i = 0; i < s.Length; i++)
            {
                value += HexToInt(s[i]) * (int)Mathf.Pow(16, (s.Length - 1) - i);
            }

            return value;
        }

    }
}
