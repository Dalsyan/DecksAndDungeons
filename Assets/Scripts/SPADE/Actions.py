import json
from shutil import which
from socket import socket as s

from spade.behaviour import *

import AgenteCarta as card
import OwlOntology

from cmath import e, sqrt
from typing import Tuple

from owlready2 import *

FREE = False
OCCUPIED = True

class Actions:
    def __init__(self, spade_socket  = None, unity_socket = None , owl = OwlOntology.OntologyActions):
        self.spade_sock = spade_socket
        self.unity_sock = unity_socket
        self.owl = owl()

    ##############################
    #                            #
    #           CARTAS           #
    #                            #
    ##############################
    
    def nearest_enemy(self, player_card_agent : card.CardAgent, enemy_card_agents : list):
        nearest = None

        for agent in enemy_card_agents:
            if nearest == None:
                nearest = agent

            elif self.dist(player_card_agent, agent) < self.dist(player_card_agent, nearest):
                nearest = agent
                
        print(f"{player_card_agent.name} nearest_enemy: {nearest.name}")
        return nearest
    
    def nearest_ally(self, player_card_agent : card.CardAgent, ally_card_agents : list):
        nearest = None

        for agent in ally_card_agents:
            if self.dist(player_card_agent, agent) > 0:
                if nearest == None:
                    nearest = agent

                elif self.dist(player_card_agent, agent) < self.dist(player_card_agent, nearest):
                    nearest = agent

        if nearest is not None:
            print(f"{player_card_agent.name} nearest_ally: {nearest.name}")
        return nearest

    def process_pos(self, pos_str):
        pos_tuple = eval(pos_str)

        return pos_tuple

    def dist(self, card_agent: card.CardAgent, other_card_agent: card.CardAgent):
        agent_pos = self.process_pos(card_agent.pos)
        other_pos = self.process_pos(other_card_agent.pos)

        x1, y1 = agent_pos[0], agent_pos[1]
        x2, y2 = other_pos[0], other_pos[1]

        dist = abs(x2 - x1) + abs(y2 - y1)

        print(f"{card_agent.name} distance with {other_card_agent.name}: {dist}")
        return dist
    
    def can_move(self, table : dict, pos : str):
        if self.is_inside_table(pos):
            if table[pos] == FREE:
                print("table pos is FREE")
            else: 
                print("table pos is OCCUPIED")
            return table[pos] == FREE
        else:
            print("pos out of table limits")
            return False
        
    def move_to_card(self, player_card_agent: card.CardAgent, other_card_agent: card.CardAgent, table: dict):
        agent_pos = self.process_pos(player_card_agent.pos)
        print(f"{player_card_agent.card.name} en pos: {agent_pos}")
        other_pos = self.process_pos(other_card_agent.pos)
        print(f"{player_card_agent.card.name} en pos: {other_pos}")
        
        x1, y1 = agent_pos[0], agent_pos[1]
        x2, y2 = other_pos[0], other_pos[1]

        rango = player_card_agent.range

        if player_card_agent.owner == "player":
            if self.dist(player_card_agent, other_card_agent) > player_card_agent.range and self.can_move(table, str((x2 + rango, y2))):
                return str((x2 + rango, y2))
            
            elif self.dist(player_card_agent, other_card_agent) > player_card_agent.range and self.can_move(table, str((x2, y2 - rango))):
                return str((x2, y2 - rango))

            elif self.dist(player_card_agent, other_card_agent) > player_card_agent.range and self.can_move(table, str((x2, y2 + rango))): 
                return str((x2, y2 + rango))
        
        elif player_card_agent.owner == "enemy":
            if self.dist(player_card_agent, other_card_agent) > player_card_agent.range and self.can_move(table, str((x2 - rango, y2))):
                return str((x2 - rango, y2))
            
            elif self.dist(player_card_agent, other_card_agent) > player_card_agent.range and self.can_move(table, str((x2, y2 - rango))):
                return str((x2, y2 - rango))

            elif self.dist(player_card_agent, other_card_agent) > player_card_agent.range and self.can_move(table, str((x2, y2 + rango))):
                return str((x2, y2 + rango))
        
        return None
            
    def is_inside_table(self, new_pos: str):
        new_pos = self.process_pos(new_pos)
        return 0 <= new_pos[0] < 6 and 0 <= new_pos[1] < 6

    def move(self, player_card_agent : card.CardAgent, table : dict, new_pos : str):
        print(f"{player_card_agent.card.name} me quiero mover a {new_pos}")
        print(f"puedo? {self.can_move(table, new_pos)}")
        if self.can_move(table, new_pos):
            table[player_card_agent.pos] = FREE
            player_card_agent.pos = new_pos
            table[new_pos] = OCCUPIED
            return True, player_card_agent.pos

        else: 
            print(f"{player_card_agent.card.name} no me puedo mover a {new_pos}")
            return False
        
    async def attack(self, player_card_agent : card.CardAgent, enemy_card_agent : card.CardAgent, damage : int, special : bool):
        if self.dist(player_card_agent, enemy_card_agent) <= player_card_agent.range:
            if special:
                damage = damage + player_card_agent.level

            if not enemy_card_agent.shielded:
                enemy_card_agent.current_hp = enemy_card_agent.current_hp - damage
            else:
                enemy_card_agent.shielded = False

    async def shield(self, player_card_agent : card.CardAgent, ally_card_agent : card.CardAgent):
        if self.Dist(player_card_agent, ally_card_agent) <= player_card_agent.range:
            ally_card_agent.shielded = True

    async def heal(self, player_card_agent : card.CardAgent, ally_card_agent : card.CardAgent, heal : int):
        if self.dist(player_card_agent, ally_card_agent) <= player_card_agent.range and ally_card_agent.current_hp < ally_card_agent.hp:
            healed_hp = 0
            while healed_hp < heal:
                ally_card_agent.current_hp += 1

    ##############################
    #                            #
    #          SOCKETS           #
    #                            #
    ##############################
    
    async def send_message_to_socket(self, msg : str):
        encoded_msg = (msg).encode()
        self.unity_sock.sendall(bytearray(encoded_msg))

    async def send_action_to_socket(self, msg : dict):
        encoded_msg = json.dumps(msg).encode()
        self.unity_sock.sendall(bytearray(encoded_msg))

    ##############################
    #                            #
    #          ONTOLOGY          #
    #                            #
    ##############################

    def get_card_role(self, card):
        role = self.owl.get_card_role(card)
        return role

    def order_cards_by_prio(self, cards : list[card.CardAgent]):
        res = cards.sort(Key = lambda x : x.prio, reverse = True)
        return res

    def search_for_card(self, name):
        card = self.owl.search_for_card(name)
        return card
        
    def search_for_deck(self, name):
        deck = self.owl.search_for_deck(name)
        return deck

    def card_to_json_action(self, card):
        card_json = self.owl.card_to_dict(card)

        return card_json

    def deck_to_json_action(self, sender, deck):
        deck_json = {'action': sender, 'data': self.owl.deck_to_list(deck) }

        return deck_json

    def create_deck(self):
        deck = self.owl.create_deck()

        return deck