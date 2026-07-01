INVENTORY SYSTEM
- slot based inventory (like minecraft)
- auto stack inventory (can't move items, same items auto stack)

VISUALS
# a class handling spawning the item UI icons (ItemFlyRewardService)
input: IdentifySO, start transform, target transform
spawning
	spawn 1 UI icon
	spawn many UI icons in a swarm
fly to the corresponding counter
callback: play pop animation on the counter after each icon reaches the counter
callback: animate the counter

# a class to animate a counter

# a view class to update a counter (ItemIconUIUpdater)
duration
target count

pull the data from inventory to update the counter right away when appears
animate the count to the target count

# a class to play pulse (pop) animation (HudAdditivePulseAnimator)

# Remaining problems
inventory data updates

## Problem 1
which counter should be updated when the inventory data changes? Which counter should be updated after the animation is finished?
-> the source should decide
1. tag
2. channel
3. rule

## Problem 2
each counter can have different animations
1. -> spawn item UI icons -> fly it to the counter -> animate the counter when the icon reaches the counter
2. -> animate the counter to the final target value
3, 4, ... more animations in the future

## Problem 3
how to handle situation where counter object can be anywhere (UI and in world)? Each counter Register itself

each IdentitySO can have many counters

ItemFlyRewardService works with both 3d and 2d
ItemFlyRewardService can spawn different objects, not just 2d image