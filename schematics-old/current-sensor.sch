EESchema Schematic File Version 4
EELAYER 30 0
EELAYER END
$Descr A4 11693 8268
encoding utf-8
Sheet 1 1
Title ""
Date ""
Rev ""
Comp ""
Comment1 ""
Comment2 ""
Comment3 ""
Comment4 ""
$EndDescr
Wire Wire Line
	5200 2800 5200 3200
Wire Wire Line
	5200 3200 5200 3300
Wire Wire Line
	5700 3800 5700 3200
Wire Wire Line
	5700 3200 5200 3200
Connection ~ 5200 3200
Text Label 5200 2800 0    10   ~ 0
+5V
Wire Wire Line
	6900 3000 6900 2900
Text Label 6900 3000 0    10   ~ 0
+5V
Wire Wire Line
	5200 4200 5200 3900
Wire Wire Line
	5200 4200 5700 4200
Wire Wire Line
	5700 4200 5700 4100
Connection ~ 5200 4200
Text Label 5200 4200 0    10   ~ 0
GND
Wire Wire Line
	4700 4200 4700 3500
Wire Wire Line
	4700 3500 5000 3500
Text Label 4700 4200 0    10   ~ 0
GND
Wire Wire Line
	7700 4300 7700 4200
Text Label 7700 4300 0    10   ~ 0
GND
Wire Wire Line
	5000 3700 4500 3700
Wire Wire Line
	4500 3700 4300 3700
Wire Wire Line
	4600 3000 4500 3000
Wire Wire Line
	4500 3000 4500 3700
Connection ~ 4500 3700
Wire Wire Line
	5000 3000 5800 3000
Wire Wire Line
	5800 3000 5800 3600
Wire Wire Line
	5800 3600 5600 3600
Wire Wire Line
	5800 3600 6300 3600
Connection ~ 5800 3600
Wire Wire Line
	3700 4300 3700 4500
Wire Wire Line
	3700 4500 3400 4500
Wire Wire Line
	3700 4500 6100 4500
Wire Wire Line
	6100 4500 6100 4000
Wire Wire Line
	6100 4000 6300 4000
Connection ~ 3700 4500
Wire Wire Line
	3700 3900 3700 3700
Wire Wire Line
	3700 3700 3400 3700
Wire Wire Line
	3700 3700 3900 3700
Connection ~ 3700 3700
Wire Wire Line
	7700 3900 7700 3800
Wire Wire Line
	7700 3800 7600 3800
Wire Wire Line
	7700 3800 8200 3800
Connection ~ 7700 3800
$Comp
L current-sensor-eagle-import:+5V #P+01
U 1 1 6A081B6E
P 5200 2700
F 0 "#P+01" H 5200 2700 50  0001 C CNN
F 1 "+5V" V 5100 2500 59  0000 L BNN
F 2 "" H 5200 2700 50  0001 C CNN
F 3 "" H 5200 2700 50  0001 C CNN
	1    5200 2700
	1    0    0    -1  
$EndComp
$Comp
L current-sensor-eagle-import:GND #GND01
U 1 1 8DA2BB24
P 5200 4300
F 0 "#GND01" H 5200 4300 50  0001 C CNN
F 1 "GND" H 5100 4200 59  0001 L BNN
F 2 "" H 5200 4300 50  0001 C CNN
F 3 "" H 5200 4300 50  0001 C CNN
F 4 "G" H 5100 4200 59  0001 L BNN "SPICEPREFIX"
	1    5200 4300
	1    0    0    -1  
$EndComp
$Comp
L current-sensor-eagle-import:741P IC1
U 1 1 22F7723E
P 5300 3600
F 0 "IC1" H 5400 3825 59  0000 L BNN
F 1 "741P" H 5400 3400 59  0000 L BNN
F 2 "current-senaor:DIL08" H 5300 3600 50  0001 C CNN
F 3 "" H 5300 3600 50  0001 C CNN
F 4 "X" H 5400 3400 59  0001 L BNN "SPICEPREFIX"
	1    5300 3600
	1    0    0    -1  
$EndComp
$Comp
L current-sensor-eagle-import:GND #GND02
U 1 1 4877C9E2
P 4700 4300
F 0 "#GND02" H 4700 4300 50  0001 C CNN
F 1 "GND" H 4600 4200 59  0001 L BNN
F 2 "" H 4700 4300 50  0001 C CNN
F 3 "" H 4700 4300 50  0001 C CNN
F 4 "G" H 4600 4200 59  0001 L BNN "SPICEPREFIX"
	1    4700 4300
	1    0    0    -1  
$EndComp
$Comp
L current-sensor-eagle-import:R-EU_0207_10 R1
U 1 1 9120CD76
P 4800 3000
F 0 "R1" H 4650 3059 59  0000 L BNN
F 1 "81k" H 4650 2870 59  0000 L BNN
F 2 "current-senaor:0207_10" H 4800 3000 50  0001 C CNN
F 3 "" H 4800 3000 50  0001 C CNN
	1    4800 3000
	1    0    0    -1  
$EndComp
$Comp
L current-sensor-eagle-import:R-EU_0207_10 R2
U 1 1 C766666B
P 4100 3700
F 0 "R2" H 4050 3559 59  0000 L BNN
F 1 "33k" H 4050 3770 59  0000 L BNN
F 2 "current-senaor:0207_10" H 4100 3700 50  0001 C CNN
F 3 "" H 4100 3700 50  0001 C CNN
	1    4100 3700
	1    0    0    -1  
$EndComp
$Comp
L current-sensor-eagle-import:R-EU_0207_10 R3
U 1 1 B785D858
P 3700 4100
F 0 "R3" H 3550 4159 59  0000 L BNN
F 1 "39" H 3550 3970 59  0000 L BNN
F 2 "current-senaor:0207_10" H 3700 4100 50  0001 C CNN
F 3 "" H 3700 4100 50  0001 C CNN
	1    3700 4100
	0    -1   -1   0   
$EndComp
$Comp
L current-sensor-eagle-import:LTC1966_MODULE U$1
U 1 1 89E33A62
P 8100 3900
F 0 "U$1" H 8100 3900 50  0001 C CNN
F 1 "LTC1966_MODULE" H 8100 3900 50  0001 C CNN
F 2 "current-senaor:LTC1966_MODULE" H 8100 3900 50  0001 C CNN
F 3 "" H 8100 3900 50  0001 C CNN
	1    8100 3900
	1    0    0    -1  
$EndComp
$Comp
L current-sensor-eagle-import:C-EU025-025X050 C1
U 1 1 CA43AFB1
P 5700 3900
F 0 "C1" H 5760 3915 59  0000 L BNN
F 1 "100n" H 5760 3715 59  0000 L BNN
F 2 "current-senaor:C025-025X050" H 5700 3900 50  0001 C CNN
F 3 "" H 5700 3900 50  0001 C CNN
	1    5700 3900
	1    0    0    -1  
$EndComp
$Comp
L current-sensor-eagle-import:GND #GND03
U 1 1 21137572
P 7100 5100
F 0 "#GND03" H 7100 5100 50  0001 C CNN
F 1 "GND" H 7000 5000 59  0001 L BNN
F 2 "" H 7100 5100 50  0001 C CNN
F 3 "" H 7100 5100 50  0001 C CNN
F 4 "G" H 7000 5000 59  0001 L BNN "SPICEPREFIX"
	1    7100 5100
	1    0    0    -1  
$EndComp
$Comp
L current-sensor-eagle-import:GND #GND04
U 1 1 12C122FD
P 6800 5100
F 0 "#GND04" H 6800 5100 50  0001 C CNN
F 1 "GND" H 6700 5000 59  0001 L BNN
F 2 "" H 6800 5100 50  0001 C CNN
F 3 "" H 6800 5100 50  0001 C CNN
F 4 "G" H 6700 5000 59  0001 L BNN "SPICEPREFIX"
	1    6800 5100
	1    0    0    -1  
$EndComp
$Comp
L current-sensor-eagle-import:C-EU025-025X050 C2
U 1 1 C33DD461
P 7700 4000
F 0 "C2" H 7760 4015 59  0000 L BNN
F 1 "1u" H 7760 3815 59  0000 L BNN
F 2 "current-senaor:C025-025X050" H 7700 4000 50  0001 C CNN
F 3 "" H 7700 4000 50  0001 C CNN
	1    7700 4000
	1    0    0    -1  
$EndComp
$Comp
L current-sensor-eagle-import:GND #GND05
U 1 1 5B6A58CA
P 7700 4400
F 0 "#GND05" H 7700 4400 50  0001 C CNN
F 1 "GND" H 7600 4300 59  0001 L BNN
F 2 "" H 7700 4400 50  0001 C CNN
F 3 "" H 7700 4400 50  0001 C CNN
F 4 "G" H 7600 4300 59  0001 L BNN "SPICEPREFIX"
	1    7700 4400
	1    0    0    -1  
$EndComp
$Comp
L current-sensor-eagle-import:+5V #P+02
U 1 1 637B3A6A
P 6900 2800
F 0 "#P+02" H 6900 2800 50  0001 C CNN
F 1 "+5V" V 6800 2600 59  0000 L BNN
F 2 "" H 6900 2800 50  0001 C CNN
F 3 "" H 6900 2800 50  0001 C CNN
	1    6900 2800
	1    0    0    -1  
$EndComp
Text Notes 3200 5200 0    59   ~ 0
IN2 of LTC1966 module is tied to a VCC/2 reference voltage through 100K resistors.
Text Notes 3200 5000 0    59   ~ 0
39ohm gives the best results over 15mA (15A detected). \nThe voltage swing will never reach 5Vpp
Text Notes 6400 5500 0    59   ~ 0
/EN is internally tied to GND
Text Notes 3200 3000 0    59   ~ 0
4.5KW means 19mA RMS detected: \n730mV RMS = 2.06Vpp:\nUse 2.4 gain.
Text Notes 3050 4100 0    50   ~ 0
1:1000
$EndSCHEMATC
