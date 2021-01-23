# BallisticCalculator

A new version of a ballistic calculator API, a new generation of a
light-weight, LGPL library of modelling projectile trajectory in atmosphere

The library provides trajectory calculation for projectiles including for various
applications, including air rifles, bows, firearms, artillery and so on.

3DF model that is used in this calculator is rooted in old C sources of version 2 of the public version of JBM
calculator, ported to C#, optimized, fixed and extended with elements described in
Litz's "Applied Ballistics" book and ideas of friendly project by Alexandre Trofimov.

The online version API documentation is located https://gehtsoft-usa.github.io/BallisticCalculator/web-content.html

The library is also available in Go: https://github.com/gehtsoft-usa/go_ballisticcalc

For those who is looking for a JavaScript version I highly recommend a project of our friend Alexandre Trofimov

The current status of the project is ALPHA version.

RISK NOTICE

The library performs very limited simulation of a complex physical process and so it performs a lot
of approximations. Therefore the calculation results MUST NOT be considered as completely and reliably
reflecting actual behavior or characteristics of projectiles. While these results may be used for
educational purpose, they must NOT be considered as reliable for the areas where incorrect calculation may
cause making a wrong decision, financial harm, or can put a human life at risk.

THE CODE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE MATERIALS OR THE USE OR OTHER DEALINGS IN THE MATERIALS.
