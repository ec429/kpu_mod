Kerbal Processing Unit
======================

KPU is a mod for the game Kerbal Space Program by Squad.
It allows you to load up your space probes with programs to allow them to
 operate autonomously.

KPU is currently in alpha test; not all features have been implemented.  The
 parts in the mod at present do not have new models; instead they are using
 copies of existing part models (the KER units for the two Kelliot processors,
 the stock Seismic Accelerometer for the FS-RA radioaltimeter, the Reflectron
 DP-10 from RemoteTech for the KNS and LORAN position sensors, etc).
 KPU's developer is actively looking for someone to create the part models.

KPU is developed by Edward Cree, who goes by the handle 'soundnfury' on IRC
 and 'ec429' elsewhere; his website is at http://jttlov.no-ip.org.  During
 the day he works on a Linux kernel device driver (in C) and a test automation
 system (in Python); before writing KPU he'd never used C#.

Because KPU contains some code based heavily on similar code in the RemoteTech
 mod (notably the attitude controller and AbstractWindow), it may be
 considered a derived work of that mod.  Consequently, it is licensed under
 the GNU General Public License, version 2.

NOTE:  There are two builds of KPU, for use with and without kappa-ray.  The
 kappa build requires the kappa-ray mod to be installed, and allows k-rads to
 trigger SEUs (Single Event Upsets) causing spurious edges and latch changes,
 which may occasionally make your probe do something different to what's in
 its programming.
 The nokappa build does not implement k-rad effects, and will function equally
 well whether kappa-ray is installed or not.


What's the point?
-----------------

When using the excellent RemoteTech mod, trying to land on a distant planet or
 moon can be infuriatingly difficult because of the speed-of-light delay.  If
 the target has an atmosphere, fragile parts like solar panels and antennas
 must be retracted for the descent, but have to be programmed to re-open after
 landing or the probe will remain uncontactable.  Doing this with RemoteTech's
 time delay system requires guessing how long the descent will take, typically
 with plenty of trial, error, and quickloading to try again.  Meanwhile,
 targets without an atmosphere are even harder as the probe has to be slowed
 down with retrograde thrust, which must be judged just right so that it runs
 out of speed and height at the same time.
In the real world, space probes have enough processing power on board to run
 at least some of their tasks semiautonomously.  Landings are often handled in
 this manner, since the control delay makes landing a planetary probe by hand
 unfeasible.  Other tasks, such as science data gathering or craft thermal
 control, are also likely to be at least partially automated, especially for a
 long-distance probe like New Horizons, whose speed-of-light delay at Pluto
 will be 4½ hours each way!
Kerbal Space Program makes some concessions towards automation, notably probe
 core SAS and the parachute deployment parameters; RemoteTech goes further by
 allowing automatic execution of maneuver nodes, as well as queuing up
 commands for execution at a specific time.  However, as mentioned above, this
 is frequently insufficient to support missions that realistically should be
 possible, especially late in the Tech Tree.  The "Automation" tech node is
 even described in-game as "letting an experimental AI handle every aspect of
 a mission", but gives the player nothing more than a probe core shaped for a
 Mk2 space-plane fuselage.
The alternative to KPU is an autopilot mod, like MechJeb, which is capable of
 landing a probe automatically.  But this takes much of the challenge out of
 the mission, because the mod does everything for you.  In KPU, you must write
 the autopilot program yourself - and add the necessary sensors to your vessel
 to provide the inputs the program needs.


What parts does this mod add?
-----------------------------

The major part category added is the Processor, which runs the program.  The
 Processor also stores the program, and has only a limited amount of IMEM
 (instruction memory) for this task.  Each token in the language takes up one
 word of IMEM (except for comments and whitespace).
There are four Processor parts.  The Kelliot 4040 is a simple edge-triggered
 processor that can only handle simple conditions, and only has 16 words of
 IMEM.  The Kelliot 8080 adds logical operators (AND, OR) allowing more
 complex programs, along with 64 words of IMEM.  The Kelliot KE-7 supports the
 full language including arithmetic operators and level-triggered actions, and
 stores up to 256 words of IMEM, but requires a lot of ElectricCharge (1.5
 charge/s) to operate.  The Kerbolics LITHP-M is far more efficient, needing
 just 0.25 charge/s, with a colossal 65,536 words of IMEM.
The other type of part in this mod is the Sensor.  A program isn't much use
 without inputs to drive it, and KPU has plenty of inputs.
Most of the sensors have to do with letting the probe work out its position,
 velocity and orientation.  There are several combinations of sensors which
 will allow this, some of which give more information than others.  For
 example, the FS-RA Doppler radioaltimeter, which also measures srfHeight,
 srfSpeed and srfVerticalSpeed, will allow orienting to Surface Prograde or
 Retrograde as well as Vertical, whereas star trackers / stellar compasses
 will allow arbitrary orientation (though some will not be able to determine
 roll, as they work by tracking a single star).  Planetary Infrared sensors
 will tell a probe where the planet (or moon) being orbited is, thus giving
 altitude and Vertical orientation, while an Inertial Platform will need to
 be recalibrated periodically by position updates from Mission Control (so an
 out-of-contact probe will gradually experience drift and lose accuracy).  The
 Kerbin Navigation System (KNS) will give position information to probes close
 enough to Kerbin to triangulate radio signals from around the globe; it will
 function up to 300km altitude.  The later LORAN sensor will do the same out
 to 3Mm - notably, this is sufficient for keostationary orbit.
There are also sensors for localGravity (using the stock Gravioli Detector)
 and gear landing sensors which detect when the vessel has touched down.
 Inputs measuring the charge level of batteries and the thrust-to-mass ratio
 of the vehicle are also available and need no special sensors.


How do I program my probe?
--------------------------

The Processor's right-click menu contains four actions: Run Program, Edit
 Program, Upload Program and Watch Display.  The last of these will open a
 window showing the current values of the Processor's inputs and outputs.
To program a probe, you must first enter the program in the KPU Code window,
 brought up by the Edit Program action.  When this is done, clicking the
 "Compile!" button will parse the program and prepare it for uploading; also,
 a message will be generated telling you how many words of IMEM the program
 requires.  Then, the Upload Program action will send the prepared program to
 the probe, and finally the Run Program action will start execution of the
 program.  If you get part-way through this process and want to back out, the
 "Undo" button in the KPU Code window will give the last program successfully
 compiled, while the "Revert" button will give the program last uploaded to
 the probe.
The program is written in a language using Forward Polish Notation, also known
 as Prefix Notation, which avoids parentheses and operator precedence (as well
 as being easy to parse ;).  In this system, operators come before the values
 they act on, and since each operator takes a defined number of values, trees
 of operators can readily be constructed.  For example, the expression
 "* + 2 3 4", read as 'the product of (the sum of 2 and 3) and 4', is
 equivalent to "(2 + 3) * 4" in the more conventional infix notation.
Statements in the language, one to a line, take one of the forms
  ON condition DO action-list
  ON condition1 HIBERNATE condition2
  IF condition THEN action-list
 where _condition_ must be a boolean-valued expression.  Lists of actions are
 formed by the ; operator, which takes two action operands and performs both.
An ON/DO statement will perform its action-list on a positive edge, i.e. when
 its condition becomes true.  An IF/THEN statement will continuously perform
 its action-list as long as its condition remains true.
An ON/HIBERNATE will, on a positive edge of condition1, put the KPU into a
 hibernation mode.  In this state, it will consume much less ElectricCharge,
 but will execute no code, merely waiting for condition2 to be true.  When it
 is, the KPU will come out of hibernation and resume normal code execution
 from the start of the program.
An action generally takes the form
  @identifier.method expression
 where _method_ will usually be "set", though numeric outputs also take "incr"
 or "decr" to smoothly slew the value.  _expression_ can be a list constructed
 with the , operator, which combines its two operands rather like a LISP cons.
 _identifier_ should be the name of one of the available outputs.  Strictly
 speaking, the whole phrase 'identifier.method' is considered by the language
 definition to be a single identifier.
The .incr and .decr methods only make sense for IF/THEN; they won't do much if
 used in an ON/DO statement.
The full syntax of the language in Backus-Naur form is as follows:
	stmt    ::= ( on-stmt | on-hiber | if-stmt | comment ) \n
	comment ::= #.*
	on-stmt ::= ON expr DO act-list
	on-hiber::= ON expr HIBERNATE expr
	if-stmt ::= IF expr THEN act-list
	act-list::= (; act-list)? action
	expr    ::= un-op expr | bin-op expr expr | ident | literal
	un-op	::= !
	bin-op  ::= log-op | comp-op | arith-op
	log-op  ::= AND | OR
	comp-op ::= < | >
	arith-op::= + | - | * | /
	ident   ::= [a-z][a-zA-Z0-9_.]*
	literal ::= -?[0-9]+(\.[0-9]+)?~?
	action  ::= @ ident expr-lst?
    expr-lst::= (, expr-lst)? expr
Some more things to note: whitespace (except for newlines) is ignored and may
 be omitted where doing so does not introduce syntactic ambiguity; when the
 program is compiled its source will also be normalised to include a single
 space after every token except '@' and '!'.  Note in particular the need for
 whitespace when an ident is followed by a token beginning with a letter or
 digit, and when the '-' operator is followed by a literal which does not
 already begin with a minus sign.
The optional trailing tilde '~' on a literal will turn it into an angle: any
 sum or difference on two angles will be done modulo 360, while a comparison
 between two angles will be done "naturally": a > b if and only if the
 positive angle from a to b is smaller than the negative.  For example,
 90~ > 0~, but 0~ > 270~ (because the negative angle is 90° while the
 positive is 270°).  Any attempt to * or / two angles will cause an error.
The output 'throttle' will take the maximum of the program's output and the
 flight input.  This allows RemoteTech to execute maneuver nodes while the KPU
 processor is running.
Here are some simple examples of the language:
	ON < batteries 10 DO @rtAntennas.set false
	ON > batteries 90 DO @rtAntennas.set true
 This will cause the probe to go into hibernation when its ElectricCharge
 reserves fall below 10%, by shutting down its antennas, and wake back up when
 the batteries reach 90% charge.
	ON < srfHeight 10000 DO @orient.set srfRetrograde
	IF <srfHeight 8000DO@throttle.set * -1000 / srfVerticalSpeed +srfHeight 40
	ON < srfHeight 250 DO @gear.set true
 Rudimentary (and untested) lander code, will probably only cope with vertical
 descents.  Constants will need tuning for the vessel's thrust-to-mass ratio
 and the gravity of the target body.
Supported values for @orient.set are:
 srfPrograde
 orbPrograde
 srfRetrograde
 orbRetrograde
 srfVertical (points normal to terrain; probably buggy though)
 orbVertical (points away from centre of planet/moon)
 , hdg pitch (holds the given heading and pitch)
 , , hdg pitch roll (holds the given heading, pitch and roll; might be buggy)
The Kelliot KE-7 and the Kerbolics LITHP-M also support 'latches', single-bit
 registers, allowing multiple distinct modes of spacecraft operation.  The
 KE-7 has two latches, while the LITHP-M has 32.  These can be used like any
 other input or output, under the names latch0, latch1 etc.  When the program
 is run, all latches are initialised to false.
All processors except the Kelliot 4040 support 'timers', allowing actions to
 be time-delayed or sequenced.  They provide the inputs and outputs timer0,
 timer1 etc.  Setting a timer to true will start it counting from the current
 time, while setting it to false will stop it.  While a timer is counting, it
 outputs the number of seconds since it was started; when stopped, it outputs
 -1.  The Kelliot 8080 has one timer, the KE-7 has two, and the Kerbolics
 LITHP-M has four.  It is actually possible to use a timer as a latch, setting
 with '@timer0.set true' and testing with '> timer0 -0.5', which may be useful
 for the 8080 which has a timer but no latches.
If an error occurs in program execution, the offending statement will be
 marked 'skip': the processor will no longer attempt to execute it.  Moreover,
 the input 'error' will be set to true.  It will revert to false after it is
 read, or after the last statement in the program.  Typical usage will be to
 put the spacecraft into a 'safe mode' by means of setting a latch:
	ON error DO @latch0.set true
 and having all potentially-destructive actions (e.g. engine burns) protected
 by putting "!latch0 AND" at the start of their condition.
Note that boolean evaluation will short-circuit, so e.g. "AND false error"
 will _not_ clear the error flag, because it will never read it.


Can I save and re-use my programs?
----------------------------------

Yes!  The KPU Program Library allows you to save programs within your KSP save
 game.  Just click the 'Save' button in the Code window (note: your code must
 be in a compiled state) and enter a name in the pop-up window.  Then, to load
 a program onto a probe, click the 'Load' button in the Code window to open
 the Program Library.  Here you will see a list of program names, and some
 information about the one currently selected (such as processor requirements
 and an editable description field).  Then just click 'Load' in the Program
 Library to load the currently selected program into the Code window.  (Note
 that it won't upload the code to the probe yet; you still have to do that
 separately with the 'Upload Program' action on the processor, and then 'Run
 Program' to execute it.)
Remember that the Program Library stores the programs in the KSP save game;
 consequently, if you start a new game, the Program Library will be empty.


Where do I report bugs?
-----------------------

Open an issue on the project's GitHub page,
	https://github.com/ec429/kpu_mod/issues
Sometimes the developer will be on IRC as soundnfury in the #kspmodders
 channel on irc.esper.net.
