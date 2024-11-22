from __future__ import annotations
from copy import deepcopy

class Point:
	def __init__(self, data: dict, rot_name: str):
		self.data = deepcopy(data)

	def __mul__(self, other: int) -> Point:
		self.data["X"] *= other
		self.data["Y"] *= other

class Rectangle:
	def __init__(self, data: dict, rot_name: str):
		if rot_name in data:
			self = Rectangle(data[rot_name])
			return

		self.data = deepcopy(data)
		self.height = self.data["Height"]