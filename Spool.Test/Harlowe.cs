using System.Linq;
using System.IO;
using System;
using Xunit;
using Spool.Harlowe;
using System.Xml.Linq;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Collections;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Spool.Test
{

    class ListStory : Story, IEnumerable<KeyValuePair<string, string>>
    {
        public Dictionary<string, string> Passages { get; } = new Dictionary<string, string>();
        public IEnumerable<string> PassageNames => Passages.Keys;
        public string Start => throw new NotImplementedException();
        public IEnumerable<string> GetTags(string passage) => Enumerable.Empty<string>();
        public string GetPassage(string name) => Passages[name];
        public (int, int) GetPassagePosition(string name) => (0, 0);
        public Context Run(Cursor output) => throw new NotImplementedException();
        public void Add(string name, string body) => Passages.Add(name, body);
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => Passages.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Passages.GetEnumerator();
    }


    public class HarloweTests
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
            new []
            {
@"|ghost>[Awoo]
(enchant: ?ghost, (text-style:'outline'))
|ghost>[Ooooh]",
@"<enchant value=""0""><outline><name value=""ghost"">Awoo</name></outline></enchant>

<enchant value=""0""><outline><name value=""ghost"">Ooooh</name></outline></enchant>"
            },
            new []
            {
@"Emily
(append: ""Emily"", ""Em"")[, my maid]",
@"Emily, my maid
"
            },
            new []
            {
@"|dress>[gown] (append: ?dress)[ from happier days]",
@"<name value=""dress"">gown from happier days</name> "
            },
            new []
            {
@"Emily? Em? (prepend: ""Emily"", ""Em"")[Miss ]",
@"Miss Emily? Miss Em? "
            },
            new []
            {
@"|dress>[gown] (prepend: ?dress)[my wedding ]",
@"<name value=""dress"">my wedding gown</name> "
            },
            new []
            {
@"A categorical catastrophe! (replace: ""cat"")[**dog**]",
@"A <strong>dog</strong>egorical <strong>dog</strong>astrophe! ",
            },
            new []
            {
@"A |heart>[song] in your heart, a |face>[song] on your face.
(replace: ?face, ?heart)[smile]",
@"A <name value=""heart"">smile</name> in your heart, a <name value=""face"">smile</name> on your face.
"
            },
            new []
            {
@"Don't you recognise me? (hidden:)|truth>[I'm your OC, brought to life!]",
@"Don't you recognise me? <name value=""truth"" />"
            },
            new []
            {
@"(link-goto: ""Enter the cellar"", ""Cellar"")",
@"<a>Enter the cellar</a>"
            },
            new []
            {
@"(link-goto: ""Cellar"")",
@"<a>Cellar</a>"
            },
            new []
            {
@"(link-undo:""Retreat"")",
@"<a>Retreat</a>"
            }
        };

        public static object[][] LinkMarkup = {
            new []
            {
@"|fan)[The overhead fan spins lazily.]

(link:""Turn on fan"")[(show:?fan)]",
@"<name value=""fan"" />

<a>Turn on fan</a>",
@"<name value=""fan"">The overhead fan spins lazily.</name>

"
            },
            new []
            {
@"(link-reveal: ""Heart"")[broken]",
@"<a>Heart</a>",
@"Heartbroken"
            },
            new []
            {
@"(set: $cheese to 0)(link-repeat: ""Add cheese"")[(set:$cheese to it + 1) You add another cheese.]",
@"<span><a>Add cheese</a></span>",
@"<span><a>Add cheese</a> You add another cheese.</span>"
            },
            new []
            {
@"But those little quirks paled before (link-show: ""her darker eccentricities"", ?twist). |twist)[She was a furry all along.]",
@"But those little quirks paled before <a>her darker eccentricities</a>. <name value=""twist"" />",
@"But those little quirks paled before her darker eccentricities. <name value=""twist"">She was a furry all along.</name>"
            }
        };
    
// TODO: (if: (num:"5") > 2)
// TODO: (either: ...$array)

// TODO: 'Named hook markup' clicks etc.
// TODO: Built-in names w/ enchant
// TODO: Hidden open hook w/ click


        [Theory]
        [MemberData(nameof(ExampleMarkup))]
        public void StaticMarkup(string input, string expected)
        {
            // using var fs = File.Open("./out.txt", FileMode.Append);
            // using var sw = new StreamWriter(fs){AutoFlush = true};
            var body = Lexico.Lexico.Parse<Block>(input
                // ,new Lexico.DelegateTextTrace(sw.WriteLine){Verbose = true}
                // ,new Lexico.Test.XunitTrace(_outputHelper){Verbose = true}
            );
            var cursor = new XCursor();
            var context = new Harlowe.Context(new ListStory {
                {"Test", input}
            }, cursor);
            context.GoTo("Test");
            var actual = cursor.Root.Root.ToString(SaveOptions.DisableFormatting);
            Assert.Equal($"<tw-passage>{expected.Replace("\r", "")}</tw-passage>", actual.Replace("\r", ""));
        }

        [Theory]
        [MemberData(nameof(LinkMarkup))]
        public void ClickableMarkup(string input, string expectedBefore, string expectedAfter)
        {
            // using var fs = File.Open("./out.txt", FileMode.Append);
            // using var sw = new StreamWriter(fs){AutoFlush = true};
            var body = Lexico.Lexico.Parse<Block>(input
                // ,new Lexico.DelegateTextTrace(sw.WriteLine){Verbose = true}
                // ,new Lexico.Test.XunitTrace(_outputHelper){Verbose = true}
            );
            var cursor = new XCursor();
            var context = new Harlowe.Context(new ListStory {
                {"Test", input}
            }, cursor);
            context.GoTo("Test");
            var actualBefore = cursor.Root.Root.ToString(SaveOptions.DisableFormatting);
            Assert.Equal($"<tw-passage>{expectedBefore.Replace("\r", "")}</tw-passage>", actualBefore.Replace("\r", ""));
            cursor.Root.Descendants(XName.Get("a")).First().Annotation<XCursor.ClickEvent>().Invoke();
            var actualAfter = cursor.Root.Root.ToString(SaveOptions.DisableFormatting);
            Assert.Equal($"<tw-passage>{expectedAfter.Replace("\r", "")}</tw-passage>", actualAfter.Replace("\r", ""));
        }

        public static object[][] ExampleExpressions = {
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
            new []{"(either: 'a', 'b', 'c') is in (a: 'a', 'b', 'c')", "true"},
            new []{"(none-pass: _num where _num < 1 or _num > 14, 6, 8, 12, 10, 9)", "true"},
            new []{"(some-pass: _num where _num < 1 or _num > 14, 6, 8, 12, 10, 9)", "false"},
            new []{"(ds: ...(shuffled: 6,5,4,3,2,1))", "(ds: 1,2,3,4,5,6)"},

            new []{"(a:1,2) is (a:1,2)", "true"},
            new []{"(a:4,5) is not (a:5,4)", "true"},
            new []{"(a:'Ape') contains 'Ape'", "true"},
            new []{"(a:(a:99)) contains (a:99)", "true"},
            new []{"(a:1,2) contains any of (a:2,3)", "true"},
            new []{"(a:1,2) contains all of (a:1,2)", "true"},
            new []{"'Ape' is in (a:'Ape')", "true"},
            new []{"(a:99) is in (a:(a:99))", "true"},
            new []{"any of (a:2,3) is in (a:1,2)", "true"},
            new []{"all of (a:1,2) is in (a:1,2)", "true"},
            new []{"(a:1,2) + (a:1,2)", "(a:1,2,1,2)"},
            new []{"(a:1,1,2,3,4,5) - (a:1,2)", "(a:3,4,5)"},
            new []{"(a: 0, ...(a:1,2,3,4), 5)", "(a:0,1,2,3,4,5)"},
            new []{"(a:'Y','Z')'s 1st", "'Y'"},
            new []{"(a:4,5)'s (2)", "5"},
            new []{"(a:5,5,5)'s length", "3"},
            new []{"1st of (a:'Y','O')", "'Y'"},
            new []{"(2) of (a:'P','S')", "'S'"},
            new []{"length of (a:5,5,5)", "3"},
            new []{"(a:2,3) matches (a: num, num)", "true"},
            new []{"(a: array) matches (a:(a:))", "true"},
            new []{"(a:2,3) is an array", "true"},
            new []{"(a:1,2)'s last", "2"},
            new []{"1st of (a:1,2)", "1"},
            // new []{"(a:1,2,3,4,5)'s 2ndto5th", "(a:2,3,4,5)"},
            new []{"all of (a:1,2) < 3", "true"},
            new []{"(a:1,2,3)'s (a:1,-1)", "(a:1,3)"},

            new []{"true is false", "false"},
            new []{"false is false", "true"},
            new []{"true is a boolean", "true"},

            // new []{"red", "#e61919"},
            // new []{"white", "#fff"},
            // new []{"black + white", "#777"},

            new []{"(dm: 'goose', 'honk') + (dm: 'robot', 'whirr')", "(dm: \"goose\", \"honk\", \"robot\", \"whirr\")"},
            new []{"(dm: \"dog\", \"woof\") + (dm: \"dog\", \"bark\")", "(dm: \"dog\", \"bark\")"},
            new []{"(dm: 'HP', 5)", "(dm: 'HP', 5)"},
            new []{"(dm: 'HP', 5) is not (dm: 'HP', 4)", "true"},
            new []{"(dm: 'HP', 5) is not (dm: 'MP', 5)", "true"},
            new []{"(dm: 'HP', 5) contains 'HP'", "true"},
            new []{"(dm: 'HP', 5) contains 5", "false"},
            new []{"'HP' is in (dm: 'HP', 5)", "true"},
            new []{"(dm:'love',155)'s love", "155"},
            new []{"love of (dm:'love',155)", "155"},
            new []{"(dm:\"Love\",2,\"Fear\",4) matches (dm: \"Love\", num, \"Fear\", num)", "true"},
            
            new []{"(ds:1,2)", "(ds:2,1)"},
            new []{"(ds:5,4) is not (ds:5)", "true"},
            new []{"(ds:'Ape') contains 'Ape'", "true"},
            new []{"(ds:(ds:99)) contains (ds:99)", "true"},
            new []{"(ds: 1,2,3) contains all of (a:2,3)", "true"},
            new []{"(ds: 1,2,3) contains any of (a:3,4)", "true"},
            new []{"'Ape' is in (ds:'Ape')", "true"},
            new []{"(a:3,4) is in (ds:1,2,3,4)", "false"},
            new []{"(ds:1,2,3) + (ds:1,2,4)", "(ds:1,2,3,4)"},
            new []{"(ds:1,2,3) - (ds:1,3)", "(ds:2)"},
            new []{"(a: 0, ...(ds:4,1,2,3), 5)", "(a: 0,1,2,3,4,5)"},
            // new []{"(ds:2,3) matches (a:3, num)", "true"},
            new []{"(ds:2,3) is a dataset", "true"},

            new []{"(datamap:'a',2,'b',4) matches (datamap:'b',num,'a',num)", "true"},
            new []{"(a: 2, 3, 4) matches (a: 2, num, num)", "true"},
            new []{"(a: (a: 2), (a: 4)) matches (a: (a: num), (a: num))", "true"},

            // new []{"(gradient: 90, 0.2, blue, 0.8, white)'s stops", "(a:(dm: \"percent\", 0.2, \"colour\", blue), (dm: \"percent\", 0.8, \"colour\", white))"},

            new []{"5 + 5", "10"},
            new []{"5 - -5", "10"},
            new []{"5 * 5", "25"},
            new []{"5 / 5", "1"},
            new []{"26 % 5", "1"},
            new []{"4 > 3.75", "true"},
            new []{"6 >= 1 + 5", "true"},
            new []{"3 < 2 * 2", "true"},
            new []{"65 <= 65", "true"},
            new []{"2 is 2", "true"},
            new []{"2 is not 0", "true"},

            new []{"'A' + 'Z'", "'AZ'"},
            new []{"'foo' is 'f'+'oo'", "true"},
            new []{"any of 'Buxom' is 'x'", "true"},
            new []{"all of 'Gadsby' is not 'e'", "true"},
            new []{"'Fear' contains 'ear'", "true"},
            new []{"'ugh' is in 'Through'", "true"},
            new []{"'YO''s 1st", "'Y'"},
            new []{"'PS''s (2)", "'S'"},
            new []{"'ear''s (a:2,3)", "'ar'"},
            new []{"1st of 'YO'", "'Y'"},
            new []{"'Contract' matches str", "true"},
            new []{"'Contract' is a str", "true"},
            new []{"last of 'foobar'", "'r'"},
            // new []{"'aeiou''s 2ndto4th", "'eio'"},
            new []{"'Penny''s length", "5"},
            new []{"all of \"aeiou\" is not \"y\"", "true"},
            new []{"'aeiou''s (a:1,-1)", "'au'"},

            new []{"(abs: -4)", "4"},
            new []{"(cos: 3.14159265)", "-1"},
            new []{"(round: (exp: 6))", "403"},
            new []{"(log: (exp:5))", "5"},
            new []{"(log10: 100)", "2"},
            new []{"(log2: 256)", "8"},
            new []{"(max: 2, -5, 2, 7, 0.1)", "7"},
            new []{"(min: 2, -5, 2, 7, 0.1)", "-5"},
            new []{"(pow: 2, 8)", "256"},
            new []{"(sign: -4)", "-1"},
            new []{"(sin: 3.14159265 / 2)", "1"},
            new []{"(sqrt: 25)", "5"},
            new []{"(round: (tan: 3.14159265 / 4))", "1"},

            new []{"(ceil: 1.1)", "2"},
            new []{"(floor: 1.99)", "1"},
            new []{"(num: \"25\")", "25"},
            new []{"(random: 1,6) is in (range:1,6)", "true"},
            new []{"(round: 1.5)", "2"},

            new []{"(lowercase: \"GrImAcE\")", "'grimace'"},
            new []{"(lowerfirst: \" College B\")", "' college B'"},
            new []{"(str-repeated: 5, \"Fool! \")", "'Fool! Fool! Fool! Fool! Fool! '"},
            new []{"(str-reversed: \"sknahT\")", "'Thanks'"},
            new []{"(str: (a: 2, \"Hot\", 4, \"U\"))", "'2,Hot,4,U'"},
            new []{"(str: ...(a: 2, \"Hot\", 4, \"U\"))", "'2Hot4U'"},
            new []{"(uppercase: \"GrImAcE\")", "'GRIMACE'"},
            new []{"(upperfirst: \" college B\")", "' College B'"},
            new []{"(words: \"god-king Torment's peril\")", "(a: \"god-king\", \"Torment's\", \"peril\")"},
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
            ).Evaluate(new Harlowe.Context(new ListStory(), new XCursor()));
            var actual = Expressions.Parse(lhs
                // , trace
            ).Evaluate(new Harlowe.Context(new ListStory(), new XCursor()));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void PassageLinkClick()
        {
            var cursor = new XCursor();
            var context = new Harlowe.Context(new ListStory {
                {"Passage 1", "[[Text->Passage 2]]"},
                {"Passage 2", "More text"}
            }, cursor);
            var screen = cursor.Root.Root;
            context.GoTo("Passage 1");
            screen.Element(XName.Get("a")).Annotation<XCursor.ClickEvent>().Invoke();
            var actual = screen.ToString(SaveOptions.DisableFormatting);
            Assert.Equal($"<tw-passage>More text</tw-passage>", actual);
        }

        [Fact]
        public void DisplayAnotherPassage()
        {
            var cursor = new XCursor();
            var context = new Harlowe.Context(new ListStory {
                {"Passage 1", "foo (display: 'Passage 2')"},
                {"Passage 2", "bar"}
            }, cursor);
            context.GoTo("Passage 1");
            var actual = cursor.Root.ToString(SaveOptions.DisableFormatting);
            Assert.Equal("<tw-passage>foo bar</tw-passage>", actual);
        }

        [Fact]
        public void TestHtmlStory()
        {
            var story = new HtmlStory(new StreamReader("../../../Spool test.html"));
            var cursor = new XCursor();
            var context = story.Run(cursor);
            context.Start();
            string Eval() => cursor.Root.Root.ToString(SaveOptions.DisableFormatting);
            Assert.Equal("<tw-passage>Some text <a>Passage 2</a></tw-passage>", Eval());
            cursor.Root.Root.Element(XName.Get("a")).Annotation<XCursor.ClickEvent>().Invoke();
            Assert.Equal("<tw-passage>More text</tw-passage>", Eval());
            Assert.Equal(new []{"foo", "bar"}, story.GetTags("Passage 1"));
            Assert.Equal(new []{"Passage 1", "Passage 2"}, story.PassageNames);
        }

        private readonly ITestOutputHelper _outputHelper;
        public HarloweTests(ITestOutputHelper outputHelper) => _outputHelper = outputHelper;
    }
}
