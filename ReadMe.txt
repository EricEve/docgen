TADS3 Documentation Generator
-----------------------------

version 0.3

copyright (c) 2003 by Edward L. Stauff

This program may be freely distributed without charge.  
All other rights are reserved.


Support
-------

As of September, 2006, Mike Roberts (mjr_@hotmail.com) has taken over
maintenance of the program.  Please contact Mike rather than Edward if
you have any questions or comments.  Please note that I'm maintaining
the program primarily for my own use to generate the Library Reference
Manual as part of the TADS 3 release process.  Beyond that, the
program is offered with NO SUPPORT and NO WARRANTY.  I might be able
to answer questions about it if time permits, but please note that I'm
not actively supporting it - any use you make of this program is at
your own risk, and you 


Overview
--------

[Edward's original overview:]

In desperate need of something resembling documentation for the TADS3 
libraries, I hacked together this C# program to read the TADS3 library 
files and generate documentation based on the comments therein. 

My parser is really stupid, and depends on the formatting of the source 
files. Some of the generated documentation is bogus, but such cases 
should be obvious. 

[Mike's additional comments:]

The parsing strategy is still based on source file formatting, but
I've made a few upgrades to catch more variations and filter out most
types of false matches.  In addition, I've introduced a preprocessor
capability that takes output from the TADS 3 compiler's "preprocess
only" mode and cross-references it with the original source.  This
lets the DocGen parser see both the original text and macro expansions
for each line of code, which makes it fairly savvy about handling the
many library macros for defining objects.


System Requirements
-------------------

This program was developed on Windows 2000.  It ought to run on any
version of Windows that supports the .Net framework, which includes
Windows XP.

You must have the .Net framework installed.  It is freely available
from Microsoft.

To compile, you'll need Microsoft Visual Studio and the C# compiler.
There's also an optional C++ component, for merging preprocessed
output with the original source comments; you'll need a C++ compiler
for this piece.  (You can probably use gcc - I don't think there's
anything Windows-specific in that piece.)


Installing DocGen
-----------------

Unzip the archive into any convenient location.
It contains the following files:

	DocGen.exe		the DocGen executable file
	Intro.html		the top-level introductory documentation file
	ReadMe.txt		the file you're reading now


Running DocGen
--------------

To run DocGen, use a command line similar to this:

	DocGen TADS3/include/*.h TADS3/lib/*.t TADS3/lib/adv3/*.t 
	TADS3/lib/adv3/*.h TADS3/lib/adv3/en_us/*.t TADS3/lib/adv3/en_us/*.h 
	-i Intro.html -v "3.0.6g" -o Output 

"TADS3" represents the directory in which the TADS3 program is installed.
"Output" represents the directory in which the documentation files will
be created.

"Intro.html" is distributed with the DocGen executable.

If you want to use the preprocessor-merge feature, which
cross-references preprocessed text against the original in order to
better handle definitions made using macros, here's what you have to
do:

  - First, preprocess each .t files using t3make -P; capture the result
    in a separate text file.  Something like this:

        t3make -q -P -I. thing.t >thing.t.P

    That produces a file called thing.t.P with all macros in thing.t
    expanded, and all #include files inserted in place.

  - Second, use ppmerge (which you need to build from ppmerge.cpp) to
    merge the original source comments into the preprocessed output.
    The t3make preprocessor strips comments out of the .P file, so
    you need to add them back.  This step also strips out the inserted
    #include files, giving you a file that has the identical line number
    structure to the original .t file, but still has all macros expanded.

       ppmerge thing.t thing.t.P mergedir\thing.t

    Note that the output file MUST have the same name as the original
    file.  This means you have to put it in a separate directory.

  - After you do this for each file, you can run DocGen as shown above.
    When you do, add the option "-m mergedir" to tell DocGen where to
    look for the preprocessor-merged copy of each file.


Revision History
----------------

8 September 2006 - MJR: numerous small parsing changes to catch more
        of the library's definition patterns; generation changes to use
        style sheets, generate a 'grammar' section, remove the superfluous
        'global variables' index.

3 August 2003 - fixed a bug which caused the program to crash if given an 
	empty ".h" file. Added a version number to the program (0.3). 

13 June 2003 - changes backslashes to slashes in HTML links. Added parsing 
	of "modify". Fixed some bugs. Began work on macro expansion (doesn't work 
	yet). 

10 June 2003 - added copies of source files; added global variables, objects 
	& functions; broke up symbol index into individual letter files; moved 
	stuff into directories. 

8 June 2003 - first version posted. 
