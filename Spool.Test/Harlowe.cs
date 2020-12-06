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
@"<link href=""Cellar"">Go to the cellar</link> is a link that goes to a passage named ""Cellar"".
<link href=""Parachuting"">Jump</link> is a link that goes to a passage named ""Parachuting"".
<link href=""Down the hatch"">Down the hatch</link> is a link that goes to a passage named ""Down the hatch""."
            },
            // TODO: Fix 'lazy' right-links
//             new [] {
// @"[[A->B->C->D->E]]",
// @"<link href=""E"">A-&gt;B-&gt;C-&gt;D-&gt;</link>"
//             },
            new [] {
@"[[A<-B<-C<-D<-E]]",
@"<link href=""A"">B&lt;-C&lt;-D&lt;-E</link>"
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
@"[2, 3, 4]"
            },
            new [] {
@"(print: (a: 2, 3, 4,))",
@"[2, 3, 4]"
            },
            new [] {
@"(print: (a: ...(a: 2, 3, 4)))",
@"[2, 3, 4]"
            },
            new [] {
@"$1.50",
"$1.50"
            },
            new [] {
@"(set: $plushieName to Felix)(put: ""pencil"" into _heldItem)
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
@"<div name=""opener"">This hook is named 'opener'</div>

<div name=""s2"">This hook is named 's2'</div>"
            },
            new [] {
@"|visible>[This hook is visible when the passage loads.]
|cloaked)[This hook is hidden when the passage loads, and needs a macro like `(show:?cloaked)` to reveal it.]

[My commanding officer - a war hero, and a charismatic face for the military.]<sight|
[Privately, I despise the man. His vacuous boosterism makes a mockery of my sacrifices.](thoughts|",
@"<div name=""visible"">This hook is visible when the passage loads.</div>
<hidden name=""cloaked"" />

<div name=""sight"">My commanding officer - a war hero, and a charismatic face for the military.</div>
<hidden name=""thoughts"" />"
            },
            new [] {
@"|1>[=
The rest of this passage is in a hook named ""1"".
|2)[==
This part is also in a hidden hook named ""2"".",
@"<div name=""1"">
The rest of this passage is in a hook named ""1"".
<hidden name=""2"" /></div>"
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
@"(set: $favouritefood to pizza)(set: $battlecry to ""Save a "" + $favouritefood + "" for me!"")$battlecry",
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
//             new []
//             {
// @"(set: $vases to 1)(set: $vases to it + 1)$vases",
// @"2"
//             },
            new []
            {
@"(put: 2 into $batteries, 4 into $bottles)
I have $batteries batteries and $bottles bottles",
@"
I have 2 batteries and 4 bottles"
            },
//             new []
//             {
// @"(put: 1 into $eggs)(put: $eggs + 2 into it)$eggs",
// @"3"
//             },
            new []
            {
@"(set: $arr to (a: 2, 3, 5))(move: $arr's 2nd into $var)$var; (print: $arr)",
@"3; [2, 5]"
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
@"(for: each _item, sword, key, scroll) [You have the _item. ]",
@"You have the sword. You have the key. You have the scroll. "
            },
//             new []
//             {
// @"(for: _ingredient where it contains ""petal"", 'apple', 'rose petal', 'orange', 'daisy petal') [Cook the _ingredient? ]",
// @"Cook the rose petal? Cook the daisy petal? "
//             },
            new []
            {
@"(for: each _i, ...(range:1,9))[_i]",
@"123456789"
            },
            new []
            {
@"(set: $cash to 250)
(set: $status to (cond: $cash >= 300, stable, $cash >= 200, lean, $cash >= 100, skint, broke))
$status",
@"

lean"
            },
            new []
            {
@"(nth: visit, 'Hi!', 'Hello again!', ""Oh, it's you!"", 'Hey!')",
@"Hi!"
            }
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
// TODO: Collapsed whitespace
// TODO: Escaped chars

// TODO: Display (needs multiple passages)

// TODO: RNG check


        [Theory]
        [MemberData(nameof(ExampleMarkup))]
        public void StaticMarkup(string input, string expected)
        {
            var passage = Lexico.Lexico.Parse<Block>(input, new Lexico.Test.XunitTrace(_outputHelper){Verbose = true});
            var context = new Context();
            passage.Render(context);
            var actual = ((XContainer)context.Screen.FirstNode).FirstNode.ToString(SaveOptions.DisableFormatting);
            Assert.Equal($"<tw-passage>{expected.Replace("\r", "")}</tw-passage>", actual.Replace("\r", ""));
        }

        
        private readonly ITestOutputHelper _outputHelper;
        public Harlowe(ITestOutputHelper outputHelper) => _outputHelper = outputHelper;
    }
}
