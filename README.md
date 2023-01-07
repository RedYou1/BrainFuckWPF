# BrainFuckWPF

You can have struct and function in a custum scripting language.
Then you can can compile it into brainfuck and use it in the IDE or in other interpreter.

# Example
```
struct Person Byte age Bool isAlive;

func NewYear Person p
{
    if p."isAlive"
    {
        add p."age" 1;
    }
}

Array persons Person 100;
foreach persons p
{
    input p;
}

input yearPassed Byte;
while yearPassed
{
    foreach persons p
    {
        NewYear p;
    }
    sub yearPassed 1;
}

foreach persons p
{
    print p."age";
}
```
