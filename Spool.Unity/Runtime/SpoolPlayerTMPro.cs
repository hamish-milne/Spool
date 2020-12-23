using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spool;
using System.IO;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class SpoolPlayerTMPro : MonoBehaviour, IPointerClickHandler
{
    public TextAsset Story;

    private Context context;

    private Spool.TMPro output = new Spool.TMPro();

    protected void Start()
    {
        var story = new HtmlStory(new StringReader(Story.text));
        context = story.Run(output);
        context.Start();
        Display();
    }

    private void Display()
    {
        GetComponent<TMP_Text>().SetText(output.Display());
    }

    public void OnPointerClick(PointerEventData eventData) {
        var cmp = GetComponent<TMP_Text>();
        var linkId = TMP_TextUtilities.FindIntersectingLink(cmp, eventData.position, eventData.pressEventCamera);
        if (linkId < 0) {
            return;
        }
        output.GetEvent<XCursor.ClickEvent>(cmp.textInfo.linkInfo[linkId].GetLinkID())?.Invoke();
        Display();
    }
}
