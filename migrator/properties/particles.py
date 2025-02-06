from copy import deepcopy

class Particle:
	def __init__(self, data: dict, rot_name: str, base_height: int):
		if rot_name in data:
			data = data[rot_name]
		
		self.data = deepcopy(data)
		self.data["Spawn Rect"]["Y"] -= base_height


class Particles:

	def __init__(self, data: dict | list, rot_name: str, base_height: int):
		self.data = []
		if type(data) is dict and rot_name in data:
			data = data[rot_name]

		if type(data) is list:
			self.data = [Particle(val, rot_name, base_height) for val in data]
		
		elif type(data) is dict:
			self.data = [Particle(data, rot_name, base_height)]

	def to_json(self):
		return [layer.data for layer in self.data]