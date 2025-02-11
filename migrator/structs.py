from __future__ import annotations
from copy import deepcopy

class Point:
	def __init__(self, data: dict):
		self.data = deepcopy(data)

	def __mul__(self, other: int) -> Point:
		self.data["X"] *= other
		self.data["Y"] *= other
		return self

class Rectangle:
	def __init__(self, data: dict, rot_name: str = ""):
		if rot_name in data:
			data = data[rot_name]

		self.data = deepcopy(data)
		self.height = self.data["Height"]
	
	def get_box(self) -> tuple:
		return (self.data["X"], self.data["Y"], self.data["Width"], self.data["Height"])
	
	