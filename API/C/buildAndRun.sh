#!/bin/bash

gcc -Wall -Wextra -Wpedantic -Werror example.c -o example -lm && ./example Data/example.phy Data/alifilter.validated.json
