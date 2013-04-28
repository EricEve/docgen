/* Copyright (c) 2006 by Michael J. Roberts.  All Rights Reserved. */
/*
Name
  ppmerge.cpp - merge a preprocessed tads 3 source with the original
Function
  Creates a preprocessed version of a tads 3 source file.  This takes the
  preprocessor output for a given source file, with macros expanded, and
  merges it with the block comments from the original source file.
Notes
  
Modified
  08/21/06 MJRoberts  - Creation
*/

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>

#define FALSE 0
#define TRUE 1


static void load_file(const char *fname, char **buf, long *len)
{
    FILE *fp;
    
    /* open the file */
    if ((fp = fopen(fname, "r")) == 0)
    {
        printf("unable to open file %s\n", fname);
        exit(1);
    }

    /* get the size */
    fseek(fp, 0, SEEK_END);
    *len = ftell(fp);
    fseek(fp, 0, SEEK_SET);

    /* allocate memory */
    if ((*buf = (char *)malloc(*len + 1)) == 0)
    {
        printf("unable to allocate memory for file %s (%lu bytes)\n",
               fname, *len);
        exit(1);
    }

    /* load it */
    *len = fread(*buf, 1, *len, fp);

    /* done with the file */
    fclose(fp);
}

void main(int argc, char **argv)
{
    FILE *fpout;
    char *orig_fname;
    size_t orig_fname_len;
    long orig_size, pre_size;
    char *pre, *orig;
    char *p, *q, *endp, *nl;
    int i, orig_cnt;
    char **orig_lines, **pre_lines;
    int in_our_file;

    /* check usage */
    if (argc != 4)
    {
        printf("usage: ppmerge <original-in> <preprocessed-in> <merged-out>\n");
        exit(1);
    }

    /* note the original filename */
    orig_fname = argv[1];
    orig_fname_len = strlen(orig_fname);

    /* open the output file */
    if ((fpout = fopen(argv[3], "w")) == 0)
    {
        printf("unable to open merged-out file %s\n", argv[3]);
        exit(1);
    }

    /* load the original and preprocessed files */
    load_file(orig_fname, &orig, &orig_size);
    load_file(argv[2], &pre, &pre_size);

    /* count lines in the original */
    for (p = orig, i = 1, endp = orig + orig_size ; p < endp ; ++p)
    {
        /* if this is a newline, count the line */
        if (*p == '\n')
            ++i;
    }

    /* allocate the line arrays */
    orig_lines = (char **)malloc(i * sizeof(char *));
    pre_lines = (char **)malloc(i * sizeof(char *));

    /* remember the maximum line number */
    orig_cnt = i;

    /* set up the line-start list */
    for (orig_lines[0] = p = orig, i = 1 ; p < endp ; ++p)
    {
        /* if this is a newline, start the next line */
        if (*p == '\n')
        {
            *p = '\0';
            orig_lines[i++] = p + 1;
        }
    }

    /* null-terminate the last line */
    *p = '\0';

    /* start with all preprocessed lines empty */
    memset(pre_lines, 0, orig_cnt * sizeof(char *));

    /* now divvy up the preprocessed lines */
    for (in_our_file = TRUE, i = 0, p = pre, endp = pre + pre_size ;
         p < endp ; p = nl + 1)
    {
        /* find the end of this line */
        for (nl = p ; nl < pre + pre_size && *nl != '\n' ; ++nl) ;

        /* null-terminate it */
        *nl = '\0';

        /* check for a #line directive */
        if (memcmp(p, "#line ", 6) == 0)
        {
            /* note whether this is our file or a separate header */
            for (q = p ; *q != '\0' && *q != '"' ; ++q) ;
            in_our_file = (memcmp(q + 1, orig_fname, orig_fname_len) == 0
                           && q[orig_fname_len + 1] == '"');

            /* note the line number */
            i = atoi(p + 6) - 1;

            /* proceed to the next line */
            continue;
        }

        /* if the current line is in our file, assign it */
        if (in_our_file && i < orig_cnt)
        {
            /* 
             *   if this line is empty so far, assign the line buffer in
             *   place; otherwise, we need to concatenate this line with the
             *   existing line 
             */
            if (pre_lines[i] == 0)
                pre_lines[i] = p;
            else
            {
                char *l = (char *)malloc(
                    strlen(pre_lines[i]) + strlen(p) + 1);
                strcpy(l, pre_lines[i]);
                strcpy(l + strlen(pre_lines[i]), p);
                pre_lines[i] = l;
            }
        }

        /* count this line */
        ++i;
    }

    /* merge the lines */
    for (i = 0 ; i < orig_cnt ; ++i)
    {
        int psp, osp;
        
        /* if we never found this preprocessed line, make it a blank line */
        if (pre_lines[i] == 0)
            pre_lines[i] = "";

        /* 
         *   if this preprocessed line is empty, and this is a comment line
         *   in the original, copy in the comment line 
         */
        for (p = pre_lines[i] ; isspace(*p) ; ++p) ;
        psp = p - pre_lines[i];
        for (q = orig_lines[i] ; isspace(*q) ; ++q) ;
        osp = q - orig_lines[i];

        if ((*p == '\0')
            && (memcmp(q, "/*", 2) == 0
                || memcmp(q, "* ", 2) == 0
                || memcmp(q, "*\n", 2) == 0
                || memcmp(q, "*. ", 3) == 0
                || memcmp(q, "*/", 2) == 0
                || memcmp(q, "//", 2) == 0))
            fputs(orig_lines[i], fpout);
        else
        {
            while (psp < osp)
            {
                static const char sp[] =
                    "                                                  "
                    "                                                  ";
                int cur;
                cur = sizeof(sp) - 1;
                if (cur > osp - psp)
                    cur = osp - psp;
                fprintf(fpout, "%.*s", cur, sp);
                psp += cur;
            }
            fputs(pre_lines[i], fpout);
        }

        fputs("\n", fpout);
    }

    /* done with the output file */
    fclose(fpout);
}
