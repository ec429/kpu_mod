Kappa-Ray
=========

Kappa-Ray is a mod for the game Kerbal Space Program by Squad.
It subjects vessels to radiation depending on their location, and applies
 various effects of that radiation.

Kappa-Ray is currently in alpha test; not all features have been implemented.

Kappa-Ray is developed by Edward Cree, who goes by the handle 'soundnfury' on
 IRC and 'ec429' elsewhere; his website is at http://jttlov.no-ip.org.

Kappa-Ray is licensed under the GNU General Public License, version 2.


Radiation Effects
-----------------
Most parts will absorb part of the radiation passing through them, thus
 generating heat.  Parts with ModuleKappaRayAbsorber will be much more
 effective at this, depending on their absorpCoeff.  (This is useful for
 shielding).  Other parts' effective absorpCoeff increases with dry mass, and
 is further increased by contained resources.  (So for instance, a full fuel
 tank - or water tank! - will absorb more than an empty one.)
Kerbals in command pods and cockpits may suffer radiation sickness, causing
 immediate death, or may develop cancer, causing delayed death.  Kerbals on
 EVA are at even greater risk, as they are completely unshielded.  The 'dose'
 displayed is the sum of event probabilities; as each probability is very
 small, this can be converted by the following continuum approximation:
 Probability = 1 - (e ^ -Dose).  See the section "Risk Assessment", below.
Solar panels will suffer degradation of their photovoltaic cells, reducing
 their power output.

More effects are planned - see roadmap-kapparay for details...


Radiation Environments
----------------------
There are three types of kappa radiation:
* Van Allen.  Low energy ambient radiation, found in a belt around planets
   with magnetic fields.  (Currently only implemented for Kerbin).  Radiation
   of this type is also found (at very high fluxes) in the Sun's corona.
   The k-ray flux of Kerbin's Van Allen belts peaks at about 600km altitude
   over the equator.  At higher latitudes, the peak flux is lower, but so is
   the altitude at which it occurs.
   Most Van Allen kappa rays are at too low an energy to harm Kerbals, though
   exposure does create slight cancer risk.  However, even these low-energy
   rays are able to degrade solar panels, perhaps halving each panel's output
   in a year.
* Direct Solar.  Medium energy radiation emitted by the Sun.  As this is
   directional, it can be effectively shielded against: simply pointing vessel
   away from sun can drastically reduce kappa flux through command pods, as
   radiation is absorbed by engines, fuel tanks etc.  Another effective shield
   is a planetary magnetosphere, busily converting the solar kappa rays into
   lower-energy Van Allen radiation.  An atmosphere will block kappa rays even
   more thoroughly.  And of course, hiding in the dark behind a planet or moon
   will shield against direct solar radiation.
   The flux of solar k-radiation varies with the inverse square of distance to
   the Sun, which is bad news for any probes to Moho.  Also, the sun's k-rad
   output varies with time; in particular, every now and then a solar storm
   will occur (on average, one every 50 days or so).  These storms last for
   two days and, at their peak, k-rad flux may exceed fifty times its normal
   level!  Fortunately, they take some time to reach their peak, giving you
   time to make the appropriate preparations.
   Solar kappa rays are right in the energy level window most likely to cause
   cancer - good shielding is a must for any kerbals undergoing prolonged
   exposure to this environment.
* Galactic.  High-energy cosmic rays.  Like Van Allen radiation, galactic
   k-rads are ambient, but unlike Van Allen radiation, they hit very hard.
   Fortunately they have a much lower flux than other sources, and are only
   found once thoroughly outside the protection of planetary magnetic fields.
   Exposure to high-energy galactic radiation carries a risk of immediate
   radiation sickness and prompt death - any unshielded time in this
   environment is highly hazardous.  However, the low rate of this radiation
   means you may cheat the reaper for a while; but sooner or later your number
   will come up.


Risk Assessment:
----------------

How worried should you be about those climbing Dose numbers?
The first number relates to risk of cancer, and the second to risk of prompt
 radiation sickness.  In each case, for a dose 'd', the probability that the
 event (i.e. cancer or radiation sickness) will occur is approximately:
    P = 1 - e^-d
The reverse calculation, the dose limit for a probability P of event, is:
    d = -ln(1 - P)
For convenience, here is a table of probability (P) versus dose (d)
|__P__|___d___|
|  5% | 0.051 |
| 10% | 0.105 |
| 25% | 0.288 |
| 50% | 0.693 |
| 75% | 1.386 |
| 90% | 2.303 |
^^^^^^^^^^^^^^^
For values of P less than 5%, you can assume that P = d.  For values of P
 greater than 90%, your Kerbals are probably all going to die; look after them
 better, you horrid callous person.

A few values for comparison / so you know what to expect:
Consider a Kerbal in a Mk1 Command Pod, with a heat shield, a 1.25m radiation
 shield, an FL-T400 tank and an LV-909 engine stacked under him.  If he does a
 Munar flyby on a free-return trajectory, while being careful to keep his
 whole stack between him and the Sun for maximum shielding, he will experience
 a total dose of (say) 0.087,0.034; he thus has about a 3.3% chance of prompt
 radiation sickness and an 8.3% chance of cancer.
If, instead, he flies the same vessel but makes no effort to point away from
 the Sun, thus getting no shielding beyond what his capsule supplies, and
 makes a landing on the Mun before returning to Kerbin, his total dose will be
 about 0.248,0.34; the risk of prompt radiation sickness is still 3.3% (as it
 depends only on the galactic radiation, nothing else being high enough energy
 to cause it), but the probability of cancer is now about 22%!  Of course, he
 might get away with it, and be completely fine.  On the other hand, he might
 not...  You will have to decide what level of risk you're comfortable with.
Remember also that the longer you spend in outer space, the more dose you will
 accumulate - even just going to Minmus will take six times as long as the Mun
 meaning six times the dose.  And the galactic radiation gets even worse as
 you head out of Kerbin's magnetosphere.  Anything truly distant and you will
 absolutely need a pod with decent shielding, probably some extra shielding
 stacked beneath it, and a helping of luck.  Remember to hide behind your
 shielding, especially if there's a solar storm, don't spend too long on EVA,
 and if you're using nuclear engines like the LV-N, be very careful indeed.

And now the good news: below about 100km, the Direct Solar and Galactic fluxes
 are both zero, and the Van Allen radiation isn't energetic enough to cause
 cancer (let alone radiation sickness).  It'll still degrade solar panels, but
 Kerbals in low orbit should be safe.


Known bugs:
-----------

Vessels other than the active one don't currently get radiation fired at them.
This is because turning that on causes the following bugs:
{
	Vessels out of physics range appear to fire their radiation at the active
	 vessel (their CoM is nonsense / in the wrong co-ordinate system).  This
	 is especially problematic because of asteroids, which are almost always
	 bathed in solar and galactic radiation.
	I don't know whether this will still happen, as we're now using the root
	 part's reference transform, rather than the vessel's CoM.  But it'll
	 probably still be broken.  (Background processing is _hard_, dangit!)

	If there are multiple vessels in close proximity, each might get some of
	 the other's radiation.  Close proximity means about 10km.
	However, it's only _likely_ to take hits if it's much closer, as all
	 radiation is aimed within a 10m sphere of the root part.
	This shouldn't be a problem, as vessels this close should have similar
	 radiation environments.  Also, the radiation isn't duplicated; if the
	 'wrong' vessel absorbs it, it won't then go on to hit the 'right' one.
	 So one vessel can shield another.
}

It's possible to have two instances of the FluxWindow open.

Kerbals don't actually die when they should; they lose their XP, and their
 camera shows K.I.A., but scene changes (?) resuscitate them.

Because of reasons, the OX-STAT fixed solar panel is no longer physicsless,
 which may unbalance some small probes with asymmetric OX-STATs.
