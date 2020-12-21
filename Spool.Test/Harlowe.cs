using System.IO;
using System;
using Xunit;
using Spool.Harlowe;
using System.Xml.Linq;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Spool.Test
{
    public class Harlowe
    {

        public static object[][] ExampleMarkup = {
//             new [] {
// "(set: $foo to (num: (str: (num: (str: 5)))))",
// ""
//             },
            new [] {
@"This is some plain text on a single line, the simplest case.",
@"This is some plain text on a single line, the simplest case."
            },
            new [] {
@"[[Go to the cellar->Cellar]] is a link that goes to a passage named ""Cellar"".
[[Parachuting<-Jump]] is a link that goes to a passage named ""Parachuting"".
[[Down the hatch]] is a link that goes to a passage named ""Down the hatch"".",
@"<a>Go to the cellar</a> is a link that goes to a passage named ""Cellar"".
<a>Jump</a> is a link that goes to a passage named ""Parachuting"".
<a>Down the hatch</a> is a link that goes to a passage named ""Down the hatch""."
            },
            // TODO: Fix 'lazy' right-links
//             new [] {
// @"[[A->B->C->D->E]]",
// @"<link href=""E"">A-&gt;B-&gt;C-&gt;D-&gt;</link>"
//             },
            new [] {
@"[[A<-B<-C<-D<-E]]",
@"<a>B&lt;-C&lt;-D&lt;-E</a>"
            },
            // TODO: Allow more constructs within links
//             new [] {
// @"[[//Seagulls!//]]",
// @"<link href=""Seagulls""><i>Seagulls!</i></link>"
//             },
            new [] {
@"//text//",
@"<i>text</i>"
            },
            new [] {
@"''text''",
@"<b>text</b>"
            },
            new [] {
@"~~text~~",
@"<s>text</s>"
            },
            new [] {
@"*text*",
@"<em>text</em>"
            },
            new [] {
@"**text**",
@"<strong>text</strong>"
            },
            new [] {
@"meters/second^^2^^",
@"meters/second<sup>2</sup>"
            },
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
            new [] {
@"(print: ""Red"" + ""belly"")",
@"Redbelly"
            },
            new [] {
@"(print: (a: 2, 3, 4))",
@"2,3,4"
            },
            new [] {
@"(print: (a: 2, 3, 4,))",
@"2,3,4"
            },
            new [] {
@"(print: (a: ...(a: 2, 3, 4)))",
@"2,3,4"
            },
            new [] {
@"$1.50",
"$1.50"
            },
            new [] {
@"(set: $plushieName to 'Felix')(put: ""pencil"" into _heldItem)
Your beloved plushie, $plushieName, awaits you after a long work day.
You put your _heldItem down and lift it for a snuggle.",
@"
Your beloved plushie, Felix, awaits you after a long work day.
You put your pencil down and lift it for a snuggle."
            },
            new [] {
@"(set: $robotText to (font: ""Courier New""))(set: _assistantText to (text-colour: ""Red""))
$robotText[Good golly! Your flesh... it's so soft!]
_assistantText[Don't touch me, please! I'm ticklish.]",
@"
<font value=""Courier New"">Good golly! Your flesh... it's so soft!</font>
<color value=""Red"">Don't touch me, please! I'm ticklish.</color>"
            },
            new [] {
@"(font: ""Courier New"")[This is a hook.

As you can see, this has a macro instance in front of it.]
This text is outside the hook.",
@"<font value=""Courier New"">This is a hook.

As you can see, this has a macro instance in front of it.</font>
This text is outside the hook."
            },
            new [] {
@"(set: $x to (font: ""Skia""))
$x[This text is in Skia.]
$x[As is this text.]",
@"
<font value=""Skia"">This text is in Skia.</font>
<font value=""Skia"">As is this text.</font>"
            },
            new [] {
@"(set: $x to 2)(if: $x is 2)[This text is only displayed if $x is 2.]",
@"This text is only displayed if 2 is 2."
            },
            new [] {
@"[This hook is named 'opener']<opener|

|s2>[This hook is named 's2']",
@"<name value=""opener"">This hook is named 'opener'</name>

<name value=""s2"">This hook is named 's2'</name>"
            },
            new [] {
@"|visible>[This hook is visible when the passage loads.]
|cloaked)[This hook is hidden when the passage loads, and needs a macro like `(show:?cloaked)` to reveal it.]

[My commanding officer - a war hero, and a charismatic face for the military.]<sight|
[Privately, I despise the man. His vacuous boosterism makes a mockery of my sacrifices.](thoughts|",
@"<name value=""visible"">This hook is visible when the passage loads.</name>
<name value=""cloaked"" />

<name value=""sight"">My commanding officer - a war hero, and a charismatic face for the military.</name>
<name value=""thoughts"" />"
            },
            new [] {
@"|1>[=
The rest of this passage is in a hook named ""1"".
|2)[==
This part is also in a hidden hook named ""2"".",
@"<name value=""1"">
The rest of this passage is in a hook named ""1"".
<name value=""2"" /></name>"
            },
            new []
            {
@"<mark>This is marked text.

&lt; So is this.
<!-- Comment -->
And this.</mark>",
@"<mark>This is marked text.

&lt; So is this.
<!-- Comment -->
And this.</mark>"
            },
            new []
            {
@"        ---
  ----
     -----",
@"<hr /><hr /><hr />"
            },
            new []
            {
@"{
    This sentence
    will be
    (set: $event to true)
    written on one line
    with only single spaces.
}",
@"This sentence will be written on one line with only single spaces. "
            },
            new []
            {
@"This line \
and this line
\ and this line, are actually just one line.",
@"This line and this line and this line, are actually just one line."
            },
            new []
            {
@"(set: $favouritefood to 'pizza')(set: $battlecry to ""Save a "" + $favouritefood + "" for me!"")$battlecry",
@"Save a pizza for me!"
            },
            new []
            {
@"(set: $altitude to 10)(set: $enemyAltitude to 8)(set: _dist to $altitude - $enemyAltitude)_dist",
@"2"
            },
            new []
            {
@"(set: $weapon to 'hands', $armour to 'naked')
Armour $armour, using $weapon",
@"
Armour naked, using hands"
            },
            new []
            {
@"(set: $vases to 1)(set: $vases to it + 1)$vases",
@"2"
            },
            new []
            {
@"(put: 2 into $batteries, 4 into $bottles)
I have $batteries batteries and $bottles bottles",
@"
I have 2 batteries and 4 bottles"
            },
            new []
            {
@"(put: 1 into $eggs)(put: $eggs + 2 into it)$eggs",
@"3"
            },
            new []
            {
@"(set: $arr to (a: 2, 3, 5))(move: $arr's 2nd into $var)$var; (print: $arr)",
@"3; 2,5"
            },
            new []
            {
@"(set: $name to ""Dracula"")
(set: $p to (print: ""Count "" + $name))
(set: $name to ""Alucard"")
$p",
@"


Count Dracula"
            },
            new []
            {
@"(set: $legs to 8)(if: $legs is 8)[You're a spider!]",
@"You're a spider!"
            },
            new []
            {
@"(set: $legs to 2)(unless: $legs is 8)[You're not a spider.]",
@"You're not a spider."
            },
            new []
            {
@"(set: $foundWand to true, $foundHat to true, $foundBeard to true)
(set: $isAWizard to $foundWand and $foundHat and $foundBeard)
$isAWizard[You wring out your beard with a quick twisting spell.]
You step into the ruined library.
$isAWizard[The familiar scent of stale parchment comforts you.]",
@"

You wring out your beard with a quick twisting spell.
You step into the ruined library.
The familiar scent of stale parchment comforts you."
            },
//             new []
//             {
// @"(set: $size to big)Your stomach makes {
// (if: $size is 'giant')[
//     an intimidating rumble! You'll have to eat plenty of trees.
// ](else-if: $size is 'big')[
//     a loud growl. You're hungry for some shrubs.
// ](else:​)[
//     a faint gurgle. You hope to scavenge some leaves.
// ]}.",
// @"Your stomach makes a loud growl. You're hungry for some shrubs."
//             },
            new []
            {
@"(set: $married to false, $date to false)$married[You hope this warrior will someday find the sort of love you know.]
(else-if: not $date)[You hope this warrior isn't doing anything this Sunday (because
you've got overtime on Saturday.)]",
@"
You hope this warrior isn't doing anything this Sunday (because
you've got overtime on Saturday.)"
            },
            new []
            {
@"(set: $isUtterlyEvil to true)
$isUtterlyEvil[You suddenly grip their ankles and spread your warm smile into a searing smirk.]
(else:)[In silence, you gently, reverently rub their soles.]
(else:)[Before they can react, you unleash a typhoon of tickles!]
(else:)[They sigh contentedly, filling your pious heart with joy.]",
@"
You suddenly grip their ankles and spread your warm smile into a searing smirk.

Before they can react, you unleash a typhoon of tickles!
"
            },
            new []
            {
@"(for: each _item, 'sword', 'key', 'scroll') [You have the _item. ]",
@"You have the sword. You have the key. You have the scroll. "
            },
            new []
            {
@"(for: _ingredient where it contains ""petal"", 'apple', 'rose petal', 'orange', 'daisy petal') [Cook the _ingredient? ]",
@"Cook the rose petal? Cook the daisy petal? "
            },
            new []
            {
@"(for: each _i, ...(range:1,9))[_i]",
@"123456789"
            },
            new []
            {
@"(set: $cash to 250)
(set: $status to (cond: $cash >= 300, 'stable', $cash >= 200, 'lean', $cash >= 100, 'skint', 'broke'))
$status",
@"

lean"
            },
            new []
            {
@"(nth: visit, 'Hi!', 'Hello again!', ""Oh, it's you!"", 'Hey!')",
@"Hi!"
            },
//             new []
//             {
// @"|ghost>[Awoo]
// (enchant: ?ghost, (text-style:'outline'))
// |ghost>[Ooooh]",
// @"<outline><name value=""ghost"">Awoo</name></outline>

// <outline><name value=""ghost"">Ooooh</name></outline>"
//             },
        };
    
// TODO: (if: (num:"5") > 2)
// TODO: (either: ...$array)

// TODO: 'Named hook markup' clicks etc.
// TODO: Built-in names w/ enchant
// TODO: Hidden open hook w/ click

// TODO: List syntax (bullet, numbered)
// TODO: data-raw attribute?
// TODO: Verbatim markup
// TODO: Aligner, column
// TODO: Heading
// TODO: Escaped chars

// TODO: Display (needs multiple passages)

// TODO: RNG check


        [Theory]
        [MemberData(nameof(ExampleMarkup))]
        public void StaticMarkup(string input, string expected)
        {
            using var fs = File.Open("./out.txt", FileMode.Append);
            using var sw = new StreamWriter(fs){AutoFlush = true};
            var passage = Lexico.Lexico.Parse<Block>(input
                // ,new Lexico.DelegateTextTrace(sw.WriteLine){Verbose = true}
                // ,new Lexico.Test.XunitTrace(_outputHelper){Verbose = true}
            );
            var context = new Context();
            passage.Render(context);
            var actual = ((XCursor)context.Cursor).Root.Root.ToString(SaveOptions.DisableFormatting);
            Assert.Equal($"<tw-passage>{expected.Replace("\r", "")}</tw-passage>", actual.Replace("\r", ""));
        }

        public static object[][] ExampleExpressions = {
            // TODO: HSL impl
            new []{"(1 + 2) / 0.25 + (3 + 2) * 0.2", "13"},
            // new []{"(hsl: 120, 0.8, 0.5)'s s", "0.8"},
            // new []{"(hsla: 28, 1, 0.4)'s h", "28"},
            new []{"(rgb: 255, 0, 47)'s b", "47"},
            new []{"(rgba: 90, 0, 0)'s r", "90"},
            new []{"(all-pass: _num where _num > 1 and _num < 14, 6, 8, 12, 10, 9)", "true"},
            new []{"(count: (a:1,2,3,2,1), 1, 2)", "4"},
            new []{"(count: 'Though', 'ugh','u','h')", "4"},
            new []{"(count: 'Though','ugh','h')", "3"},
            new []{"(count: 'Though','h') - (count: 'Though','ugh')", "1"},
            new []{"(datapairs: (dm:'B',24, 'A',25))", "(a: (dm: 'name', 'A', 'value', 25), (dm: 'name', 'B', 'value', 24))"},
            new []{"(datanames: (dm:'B','Y', 'A','X'))", "(a: 'A','B')"},
            new []{"(datavalues: (dm:'B',24, 'A',25))", "(a: 25,24)"},
            new []{"(find: _item where _item's 1st is 'A', 'Thorn', 'Apple', 'Cryptid', 'Anchor')", "(a: 'Apple', 'Anchor')"},
            new []{"(find: _num where (_num >= 12) and (it % 2 is 0), 9, 10, 11, 12, 13, 14, 15, 16)", "(a: 12, 14, 16)"},
            new []{"(interlaced: (a: 'A', 'B', 'C', 'D'), (a: 1, 2, 3))", "(a: 'A',1,'B',2,'C',3)"},
            new []{"(range:1,14)", "(a:1,2,3,4,5,6,7,8,9,10,11,12,13,14)"},
            new []{"(range:2,-2)", "(a:-2,-1,0,1,2)"},
            new []{"(dataset: ...(range:2,6))", "(dataset: 2,3,4,5,6)"},
            new []{"(repeated: 5, false)", "(a: false, false, false, false, false)"},
            new []{"(repeated: 3, 1,2,3)", "(a: 1,2,3,1,2,3,1,2,3)"},
            new []{"(reversed: 1,2,3,4)", "(a: 4,3,2,1)"},
            new []{"(rotated: 1, 'A','B','C','D')", "(a: 'D','A','B','C')"},
            new []{"(rotated: -2, 'A','B','C','D')", "(a: 'C','D','A','B')"},
            new []{"(sorted: 'A','C','E','G', 2, 1)", "(a: 1, 2, 'A', 'C', 'E', 'G')"},
        };

        [Theory]
        [MemberData(nameof(ExampleExpressions))]
        public void Assertions(string lhs, string rhs)
        {
            // using var fs = File.Open("./out.txt", FileMode.Append);
            // using var sw = new StreamWriter(fs){AutoFlush = true};
            // var trace = new Lexico.DelegateTextTrace(sw.WriteLine){Verbose = true};
            // var trace = new Lexico.Test.XunitTrace(_outputHelper){Verbose = true};
            var expected = Expressions.Parse(rhs
                // , trace
            ).Evaluate(new Context());
            var actual = Expressions.Parse(lhs
                // , trace
            ).Evaluate(new Context());
            Assert.Equal(expected, actual);
        }

        private readonly ITestOutputHelper _outputHelper;
        public Harlowe(ITestOutputHelper outputHelper) => _outputHelper = outputHelper;
    }
}
