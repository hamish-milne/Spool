using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Spool
{
    [RequireComponent(typeof(TMP_Text), typeof(Spool.Player))]
    public class TMProOutput : MonoBehaviour, IPointerClickHandler
    {
        private readonly Spool.TMPro formatter = new Spool.TMPro();
        private Spool.TMPro.State state;
        protected void StoryStart() => Display();

        private void Display()
        {
            state = formatter.Display(GetComponent<Player>().Content);
            GetComponent<TMP_Text>().SetText(state.Text);
        }

        public void OnPointerClick(PointerEventData eventData) {
            var cmp = GetComponent<TMP_Text>();
            var linkId = TMP_TextUtilities.FindIntersectingLink(cmp, eventData.position, eventData.pressEventCamera);
            if (linkId < 0) {
                return;
            }
            state.Event<XCursor.ClickEvent>(cmp.textInfo.linkInfo[linkId].GetLinkID());
            Display();
        }
    }
}
