#    AliFilter: A Machine Learning Approach to Alignment Filtering
#
#    by Giorgio Bianchini, Rui Zhu, Francesco Cicconardi, Edmund RR Moody
#
#    Copyright (C) 2024  Giorgio Bianchini
#
#    This program is free software: you can redistribute it and/or modify
#    it under the terms of the GNU General Public License as published by
#    the Free Software Foundation, version 3.
#
#    This program is distributed in the hope that it will be useful,
#    but WITHOUT ANY WARRANTY; without even the implied warranty of
#    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#    GNU General Public License for more details.
#
#    You should have received a copy of the GNU General Public License
#    along with this program.  If not, see <https://www.gnu.org/licenses/>.

import Bio.AlignIO
import alifilter

alignment = Bio.AlignIO.read("Data/example.phy", format="phylip-relaxed")
afModel = alifilter.AliFilterModel("Data/alifilter.validated.json")

mask = afModel.getMask(alignment)

print(mask)

filteredAlignment = alignment.filter(afModel) # or afModel.filter(alignment)

print(filteredAlignment)
