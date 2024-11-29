from copy import deepcopy

from structs import Rectangle

class Layer:
	def __init__(self, data: dict, rot_name: str, base_height: int):
		if rot_name in data:
			self = Layer(data[rot_name], rot_name, base_height)
			return
		
		self.data = deepcopy(data)
		
		h_diff = self.data["Source Rect"]["Height"] - base_height
		if "Draw Pos" in self.data:
			
			self.data["Draw Pos"]["Y"] += h_diff
		else:
			self.data["Draw Pos"] = {"X": 0, "Y": h_diff}


class Layers:

	def __init__(self, data: dict | list, rot_name: str, base_height: int):
		self.data = []
		if type(data) is list:
			self.data = [Layer(data[i], rot_name, base_height) for i in range(data)]
		
		elif type(data) is dict:
			if rot_name in data:
				self = Layers(data[rot_name], rot_name, base_height)
				return
			
			self.data = [Layer(data, rot_name, base_height)]
	
	def add_base(self, source_rect: Rectangle):
		self.data.insert(0, {"Source Rect": source_rect.data, "Depth": 0})

	def to_json(self):
		return [layer.data for layer in self.data]