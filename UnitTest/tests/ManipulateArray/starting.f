include wadefsrg;
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
        call NewYear p;
    }
    sub yearPassed 1;
}

foreach persons p
{
    print p."age";
}