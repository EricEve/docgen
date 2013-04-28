#  NMAKE makefile for building the Library Reference Manual.  
#
# This builds all program components and then builds the library reference
# manual based on the Adv3 library files.
#
# You'll need the Microsoft C# and C++ compilers to use this makefile.
# Also, you'll need a standard Linux-style "sed" program - you can get
# one of these from the Cygwin distribution.
#
# Before running this makefile, you should check the 


#
# Build configuration - DEBUG or release
# 
!ifdef DEBUG
CFLAGS_DBG = -Zi -Od
LDFLAGS_DBG = /debug
CSCFLAGS_DBG = /debug /define:DEBUG;TRACE
!else
CFLAGS_DBG =
LDFLAGS_DBG =
CSCFLAGS_DBG =
!endif

#
# Directory configuration
#
TADSROOT = ..\..\..
TADSBUILD = $(TADSROOT)\build
T3LIB = $(TADSBUILD)\lib
T3INC = $(TADSBUILD)\include
ADV3LIB = $(TADSBUILD)\lib\adv3
HTMLOUTDIR = $(TADSBUILD)\doc\libref

# local output directories
BINDIR = bin\Debug
MERGEDIR = ppmerge

# 
# Tool configuration
#
CSCFLAGS = \
   /incremental /baseaddress:285212672 /filealign:4096 /warn:4 \
   $(CSCFLAGS_DBG)
CSC = csc

CC = cl
CFLAGS = $(CFLAGS_DBG)
LD = link
LDFLAGS = $(LDFLAGS_DBG)

#
# Main target
#
all: $(HTMLOUTDIR)\ClassIndex.html

# cleanup
clean:
    if exist $(MERGEDIR)\*.t del $(MERGEDIR)\*.t
    if exist $(MERGEDIR)\*.t.p del $(MERGEDIR)\*.t.p
    if exist $(BINDIR)\*.obj del $(BINDIR)\*.obj
    if exist $(BINDIR)\*.exe del $(BINDIR)\*.exe
    if exist $(BINDIR)\*.bat del $(BINDIR)\*.bat

# Adv3 and T3 system library source list
LIBSOURCES = \
    $(T3LIB)\*.t \
    $(T3INC)\*.h \
    $(ADV3LIB)\*.t \
    $(ADV3LIB)\*.h \
    $(ADV3LIB)\en_us\*.t \
    $(ADV3LIB)\en_us\*.h

# the Library Reference Manual
$(HTMLOUTDIR)\ClassIndex.html: \
        $(BINDIR)\docgen.exe \
        $(LIBSOURCES) \
        $(MERGEDIR)\thing.t \
        $(BINDIR)\verid.txt \
        libref.css \
        *.jpg \
        Intro.html
    $(BINDIR)\docgen -m $(MERGEDIR) -i Intro.html -o $(HTMLOUTDIR) \
        -vf $(BINDIR)\verid.txt $(LIBSOURCES)
    copy libref.css $(HTMLOUTDIR)\*.*
    copy *.jpg $(HTMLOUTDIR)\*.*

# extract version information from the Adv3 library
$(BINDIR)\verid.txt: $(ADV3LIB)\modid.t $(BINDIR)\getver.bat
    for /f %%i in ('$(BINDIR)\getver $(ADV3LIB)\modid.t') do echo %%i>$@

$(BINDIR)\getver.bat: makefile
    rem <<$(BINDIR)\getver.bat
@grep -E "^    version = '[0-9]+[.][0-9]+[.][0-9]+([.][0-9]+)?'" %1 | sed -r "s/ +version = '(.+)'/\1/"
<<KEEP

# preprocessor merging
$(MERGEDIR)\thing.t: \
        $(BINDIR)\ppmerge.exe \
        $(ADV3LIB)\*.t \
        $(ADV3LIB)\en_us\*.t \
        $(BINDIR)\do_merge.bat
    $(BINDIR)\do_merge

$(BINDIR)\do_merge.bat: makefile
    rem <<$(BINDIR)\do_merge.bat

set origdir=%CD%
pushd $(ADV3LIB)
for %%i in (*.t) do (
  t3make -q -P -I. %%i > %origdir%\$(MERGEDIR)\%%i.p
  %origdir%\$(BINDIR)\ppmerge %%i %origdir%\$(MERGEDIR)\%%i.p %origdir%\$(MERGEDIR)\%%i
  del %origdir%\$(MERGEDIR)\%%i.p
)

cd en_us
for %%i in (*.t) do (
  t3make -q -P -I. -I.. %%i > %origdir%\$(MERGEDIR)\%%i.p
  %origdir%\$(BINDIR)\ppmerge %%i %origdir%\$(MERGEDIR)\%%i.p %origdir%\$(MERGEDIR)\%%i
  del %origdir%\$(MERGEDIR)\%%i.p
)

popd

<<KEEP
    
# DocGen program
$(BINDIR)\docgen.exe: *.cs
    $(CSC) $(CSCFLAGS) /out:$@ $**

# preprocessor merge program
$(BINDIR)\ppmerge.exe: $(BINDIR)\ppmerge.obj
    $(LD) $(LDFLAGS) /out:$@ $**

$(BINDIR)\ppmerge.obj: ppmerge.cpp

# generic rules
.SUFFIXES:.cpp.obj

.cpp{$(BINDIR)}.obj:
    $(CC) $(CFLAGS) -c -Fo$@ $<

