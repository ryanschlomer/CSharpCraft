
export class Physics {
    constructor() {

    }
    update(dt, player, world) {
        this.detectCollisions(player, world);
    }

    detectCollisions(player, world) {
        const candidates = this.boadPhase(player, world);
        const collisions = this.narrowPhase(candidates, player);

        if (collisions.length > 0) {
            this.resolveCollisions(collisions);
        }
    }


}

export class Player {
    constructor(scene) {

    }
}