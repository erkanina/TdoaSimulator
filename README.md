# TdoaSimulator
Trilateration Simulator based on Time Difference of Arrival
-----------------------------------------------------------

It's a simle Tdoa (Time Difference of Arrival) test implementation.

There's a thread in "Form1.cs" which operates in every 1-second. At first it inits Anchor ID and
locations as below;

m_Tri.AnchorAdd(100, 0, 0, 0);

m_Tri.AnchorAdd(101, 2000, 0, 2000);

m_Tri.AnchorAdd(102, 2000, 2000, 0);

m_Tri.AnchorAdd(103, 0, 2000, 2000);

m_Tri.AnchorAdd(104, 0, 0, 2000);

This is our moving test object, at initial position;

double x = -2000, y = 1000, z = 0;

In every one second test object move along x-direction

x += 100;

After reaching 2000, it resets the locations to initial position

if (x > 2000) x = -1000;

By this code you can compare fed and calculated x,y,z

For mathematical background please refer my stackoverflow mesage;
https://math.stackexchange.com/a/2489798/495743
