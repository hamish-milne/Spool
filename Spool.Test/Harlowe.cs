using System;
using Xunit;
using Spool.Harlowe;
using System.Xml.Linq;

namespace Spool.Test
{
    public class Harlowe
    {

        public static object[][] ExampleMarkup = {
            new [] {
@"This is some plain text on a single line, the simplest case.",
@"This is some plain text on a single line, the simplest case."
            },
            new [] {
@"[[Go to the cellar->Cellar]] is a link that goes to a passage named ""Cellar"".
[[Parachuting<-Jump]] is a link that goes to a passage named ""Parachuting"".
[[Down the hatch]] is a link that goes to a passage named ""Down the hatch"".",
@"<link href=""Cellar"">Go to the cellar</link> is a link that goes to a passage named ""Cellar"".
<link href=""Parachuting"">Jump</link> is a link that goes to a passage named ""Parachuting"".
<link href=""Down the hatch"">Down the hatch</link> is a link that goes to a passage named ""Down the hatch""."
            },
            new [] {
@"[[A->B->C->D->E]]",
@"<link href=""E"">A-&gt;B-&gt;C-&gt;D-&gt;</link>"
            },
            new [] {
@"[[A<-B<-C<-D<-E]]",
@"<link href=""A"">B&lt;-C&lt;-D&lt;-E</link>"
            },
//             new [] {
// @"[[//Seagulls!//]]",
// @"<link href=""Seagulls""><i>Seagulls!</i></link>"
//             },
//             new [] {
// @"//text//",
// @"<i>text</i>"
//             },
//             new [] {
// @"''text''",
// @"<b>text</b>"
//             },
//             new [] {
// @"~~text~~",
// @"<s>text</s>"
//             },
//             new [] {
// @"*text*",
// @"<em>text</em>"
//             },
//             new [] {
// @"**text**",
// @"<strong>text</strong>"
//             },
//             new [] {
// @"meters/second^^2^^",
// @"meters/second<sup>2</sup>"
//             },
            new [] {
@"(print: 54)",
@"54"
            },
            new [] {
@"(p-r-i-n-t: 54)",
@"54"
            },
            new [] {
@"(PRINT: 54)",
@"54"
            },
//             new [] {
// @"(print: ""Red"" + ""belly"")",
// @"Redbelly"
//             }
        };
    
// TODO: (if: (num:"5") > 2)
// TODO: (a: 2, 3, 4,)
// TODO: (either: ...$array)

        [Theory]
        [MemberData(nameof(ExampleMarkup))]
        public void LinkMarkup(string input, string expected)
        {
            var passage = Lexico.Lexico.Parse<Passage>(input);
            var context = new Context();
            passage.Render(context);
            var actual = context.Screen.ToString(SaveOptions.DisableFormatting);
            Assert.Equal($"<passage>{expected}</passage>", actual);
        }
    }
}