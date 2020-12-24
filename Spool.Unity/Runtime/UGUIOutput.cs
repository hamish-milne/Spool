using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spool
{
    [RequireComponent(typeof(UnityEngine.UI.Text), typeof(Spool.Player))]
    public class UGUIOutput : MonoBehaviour, IPointerClickHandler
    {
        private readonly Spool.UGUI formatter = new Spool.UGUI();
        private Spool.UGUI.State state;
        protected void StoryStart() => Display();

        private void Display()
        {
            state = formatter.Display(GetComponent<Player>().Content);
            GetComponent<Text>().text = state.Text;
        }

        public void OnPointerClick(PointerEventData eventData) {
            var cmp = GetComponent<Text>();
            var textInfo = cmp.cachedTextGenerator;
            var rt = GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, null, out var pos);
            var line = textInfo.lines.Select((l, i) => (l, i))
                .FirstOrDefault(x => pos.y > (x.l.topY - x.l.height) && pos.y <= x.l.topY);
            if (line.l.height <= 0f) {
                return;
            }
            var lineLength = line.i >= (textInfo.lineCount - 1)
                ? textInfo.characterCountVisible - line.l.startCharIdx
                : (1 + textInfo.lines[line.i + 1].startCharIdx - line.l.startCharIdx);
            var character = textInfo.characters
                .Select((c, i) => (c, i))
                .Skip(line.l.startCharIdx)
                .Take(lineLength)
                .FirstOrDefault(x => pos.x >= x.c.cursorPos.x && pos.x < (x.c.cursorPos.x + x.c.charWidth));
            if (character.c.charWidth <= 0) {
                return;
            }
            state.Event<XCursor.ClickEvent>(character.i);
            Display();
        }
    }
}
