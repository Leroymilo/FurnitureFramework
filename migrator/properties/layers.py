from copy import deepcopy

from structs import Rectangle

class Layer:
	def __init__(self, data: dict, rot_name: str, base_height: int):
		if rot_name in data:
			data = data[rot_name]
		
		self.data = deepcopy(data)
		
		try:
			rect = Rectangle(self.data["Source Rect"], rot_name)
		except KeyError: raise KeyError

		h_diff = rect.height - base_height
		if "Draw Pos" in self.data:
			self.data["Draw Pos"]["Y"] += h_diff
		else:
			self.data["Draw Pos"] = {"X": 0, "Y": h_diff}


class Layers:

	def __init__(self, data: dict | list, rot_name: str, base_height: int):
		self.data: list[Layer] = []

		if type(data) is dict and rot_name in data:
			data = data[rot_name]
		if type(data) is dict: data = [data]
		
		for val in data:
			try:
				self.data.append(Layer(val, rot_name, base_height))
			except KeyError: pass

	def to_json(self) -> list[dict]:
		return [layer.data for layer in self.data]