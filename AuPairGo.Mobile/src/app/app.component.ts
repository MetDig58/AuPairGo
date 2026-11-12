import { Component, inject } from '@angular/core';
import { IonRouterOutlet, IonApp, AnimationController, type Animation } from '@ionic/angular';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  imports: [IonApp, IonRouterOutlet],
})
export class AppComponent {
  private animationCtrl = inject(AnimationController);

  slowerTransition = (baseEl: HTMLElement, opts?: any): Animation => {
    const DURATION = 500;

    if (!opts || !opts.enteringEl || !opts.leavingEl) {
      return this.animationCtrl.create();
    }

    const enteringAnimation = this.animationCtrl
      .create()
      .addElement(opts.enteringEl)
      .duration(DURATION)
      .easing('ease-in-out')
      .fromTo('opacity', 0, 1);

    const leavingAnimation = this.animationCtrl
      .create()
      .addElement(opts.leavingEl)
      .duration(DURATION)
      .easing('ease-in-out')
      .fromTo('opacity', 1, 0);

    return this.animationCtrl.create().addAnimation([enteringAnimation, leavingAnimation]);
  };
}
