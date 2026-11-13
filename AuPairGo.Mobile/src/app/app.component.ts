import { Component, inject } from '@angular/core';
import { IonRouterOutlet, IonApp, AnimationController, type Animation } from '@ionic/angular';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  imports: [IonApp, IonRouterOutlet],
})
export class AppComponent {
  private animationCtrl = inject(AnimationController);

  scaleFadeTransition = (baseEl: HTMLElement, opts?: any): Animation => {
    const DURATION = 350;
    const EASING = 'ease-out';

    if (!opts || !opts.enteringEl || !opts.leavingEl) {
      return this.animationCtrl.create();
    }

    const enteringAnimation = this.animationCtrl
      .create()
      .addElement(opts.enteringEl)
      .duration(DURATION)
      .easing(EASING)
      .fromTo('transform', 'scale(0.95)', 'scale(1)')
      .fromTo('opacity', 0, 1);

    const leavingAnimation = this.animationCtrl
      .create()
      .addElement(opts.leavingEl)
      .duration(DURATION)
      .easing(EASING)
      .fromTo('transform', 'scale(1)', 'scale(1)')
      .fromTo('opacity', 1, 0);

    return this.animationCtrl.create().addAnimation([enteringAnimation, leavingAnimation]);
  };
}
