# BallisticCalculator

The library provides trajectory calculations for projectiles, 
including for various applications, including air rifles, 
bows, firearms, artillery, and so on.

3DF (degrees of freedom) model that is used in this calculator 
is rooted in [old C sources](http://www.jbmballistics.com/ballistics/downloads/downloads.shtml) 
of version 2 of the public version of the JBM calculator, 
ported to C#, optimized, fixed, and extended with elements 
described in Litz's "Applied Ballistics" book and ideas of the friendly 
project by Alexandre Trofimov.

Changes made since porting original C sources:

* "Deciphering" formulas and making them readable for anyone familiar
   with the school curriculum of physics.

* New drag calculation method with higher 
  accuracy (40+ approximation points for calculating polynomial coefficients vs 5-6).

* New atmosphere parameters calculating basing on NASA formulas

* New algorithm of step size definition to find the 
  right balance between performance and accuracy.

* Drift calculation is added using Liltz's formulas

* Accuracy of the calculation is withing 0.5%/0.2moa 
  (less than 2inch per 1000 yards) of the most modern commerical 
  calculators.

Please refer to [online version of the documentation](http://docs.gehtsoftusa.com/BallisticCalculator1/)

The source code of the package is available [at github](https://github.com/gehtsoft-usa/BallisticCalculator1)

