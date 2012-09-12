This program checks all the files in its current working directory.
It looks for raw footage created by the capture program FRAPS, which names 
are in the following format:
GameName year-month-date hour-minute-second-100th of a second.avi
or in other words
abCD123 0123-45-67 89-01-23-45.avi

The program VirtualDub will consider segments linked and automatically append them
if they all have the same name ending with .[number].avi (xxx.00.avi, xxx.01.avi, etc.)

So what this program does is check the timestamps and modifies the names
so that Virtual Dub will consider the segments linked. It considers two segments to be linked
if they have the same GameName and are less than an arbitrary duration apart.
This is by default 5 minutes and can be overridden with command-line parameter.

This is simply so that I don't have to append them manually which is tedious and error-prone.