import {Component, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, RouterModule} from "@angular/router";
import {AuthService} from "../../core/services/auth.service";
import {User} from "../../core/models/user.model";
import {ActivityGraphComponent} from "../../shared/components/activity-graph/activity-graph.component";
import {WorkoutService} from "../../core/services/workout.service";
import {Workout} from "../../core/models/workout.model";
import {NgForOf, NgIf, NgOptimizedImage} from "@angular/common";
import {UserService} from "../../core/services/user.service";
import {of, Subscription} from "rxjs";
import {FriendsService} from "../../core/services/friends.service";
import {FriendshipStatus} from "../../core/models/friendship-status.model";
import {switchMap} from "rxjs/operators";
import {MatIcon} from "@angular/material/icon";
import {MatProgressSpinner} from "@angular/material/progress-spinner";
import {ImageService} from "../../core/services/image.service";
import {SnackbarService} from "../../core/services/snackbar.service";

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [RouterModule, ActivityGraphComponent, NgIf, MatIcon, NgForOf, MatProgressSpinner, NgOptimizedImage],
  templateUrl: './user-profile.component.html',
  styleUrl: './user-profile.component.scss'
})
export class UserProfileComponent implements OnInit, OnDestroy {
    workouts: Workout[] | null = null;
    // Only friends can see someone's workouts; the API answers 403 otherwise.
    activityHidden = false;
    User: User = {} as User;
    isCurrentUser: boolean = true;
    friendshipStatus: FriendshipStatus = FriendshipStatus.None;
    currentUser: User = this.route.snapshot.data['user'];

    showUploadOverlay: boolean = false;
    uploadingImage: boolean = false;
    profileImageUrl: string | null = null;

    // Requests for the profile on screen. Angular reuses this component when only the username in
    // the URL changes, so they are cancelled then, before an older answer can overwrite the new profile.
    private profileRequests = new Subscription();

    constructor(private authService: AuthService,
                private workoutService: WorkoutService,
                private userService: UserService,
                private friendsService: FriendsService,
                private route: ActivatedRoute,
                private imageService: ImageService,
                private snackbarService: SnackbarService)
    {
    }

    ngOnInit(): void {

      this.route.paramMap
        .pipe(
          switchMap(params => {
            this.resetProfile();
            const username = params.get('username')!;
            // Usernames are case-insensitive.
            this.isCurrentUser = username.toLowerCase() === this.currentUser.username.toLowerCase();
            return this.isCurrentUser ? of(this.currentUser) : this.userService.getUserByUsername(username);
          })
        )
        .subscribe(user => {
          this.User = user;

          if (this.User.profileImageKey) {
            this.loadProfileImage(this.User.profileImageKey);
          }

          if(!this.isCurrentUser) this.getFriendshipStatus();
          this.fetchWorkouts();
        });
    }


    ngOnDestroy(): void {
      this.profileRequests.unsubscribe();
    }

    private resetProfile(): void {
      this.profileRequests.unsubscribe();
      this.profileRequests = new Subscription();
      this.User = {} as User;
      this.workouts = null;
      this.activityHidden = false;
      this.profileImageUrl = null;
      this.friendshipStatus = FriendshipStatus.None;
      this.showUploadOverlay = false;
    }

    onProfileImageSelected(event: Event): void {
      const input = event.target as HTMLInputElement;
      if (!input.files || input.files.length === 0) return;

      const file = input.files[0];

      // Validate file
      if (!this.validateImage(file)) return;

      this.uploadProfileImage(file);
    }

    private validateImage(file: File): boolean {
      const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png'];
      const maxSize = 5 * 1024 * 1024; // 5MB

      if (!allowedTypes.includes(file.type)) {
        this.snackbarService.error('Please upload a valid image (JPEG, JPG, PNG)');
        return false;
      }

      if (file.size > maxSize) {
        this.snackbarService.error('Image size must be less than 5MB');
        return false;
      }

      return true;
    }

    private uploadProfileImage(file: File): void {
      this.uploadingImage = true;

      this.imageService.uploadImage(file).subscribe({
        next: (response) => {
          // Update user profile with new image key
          this.updateUserProfile(response.fileKey);
        },
        error: (error) => {
          console.error('Upload failed:', error);
          this.snackbarService.error('Failed to upload image');
          this.uploadingImage = false;
        }
      });
    }

    private updateUserProfile(imageKey: string): void {
      this.User.profileImageKey = imageKey;
      this.loadProfileImage(imageKey);
      this.uploadingImage = false;
      this.snackbarService.success('Profile picture updated!');
    }

    private loadProfileImage(imageKey: string): void {
      this.profileRequests.add(this.imageService.getImageUrl(imageKey).subscribe({
        next: (response) => {
          this.profileImageUrl = response.url;
        },
        error: (error) => {
          console.error('Failed to load profile image:', error);
        }
      }));
    }


  get activityStartDate(): Date{
      const pastDate = new Date();
      pastDate.setDate(new Date().getDate() - 364);
      return pastDate;
    }

   fetchWorkouts(): void {
    const activityStartDate = this.activityStartDate;
    const today = new Date();

    if(this.isCurrentUser){
      this.profileRequests.add(this.workoutService
        .getAllInDateRange(activityStartDate, today)
        .subscribe(data => {
          this.workouts = data;
        }));
    }
    else{
      this.profileRequests.add(this.workoutService
        .getForUser(this.User.id, activityStartDate, today)
        .subscribe({
          next: data => {
            this.workouts = data;
            this.activityHidden = false;
          },
          error: () => {
            this.workouts = null;
            this.activityHidden = true;
          }
        }));
    }

  }

  // The year defaults to the one the month falls in within the last 12 months.
  workoutsPerMonth(month: number = (new Date()).getMonth(),
                   year: number = new Date().getFullYear() - (month > new Date().getMonth() ? 1 : 0)) : number {
      return this.workouts?.filter(w =>  {
        const wdate = new Date(w.date);
        return wdate.getMonth() == month && wdate.getFullYear() == year;}
      ).length ?? 0
  }

  maxWorkoutsAMonth() : number {
      let max = 0;
      for(let i = 0; i < 12; i++){
        max = Math.max(max, this.workoutsPerMonth(i))
      }
      return max
  }

  sendFriendRequest(){
      this.friendsService.sendFriendRequest(this.User.id).subscribe({
        next: () => this.friendshipStatus = FriendshipStatus.PendingSent
      });
  }

  acceptFriendRequest() {
      this.friendsService.acceptFriendRequest(this.User.id).subscribe( {
        next: () => {
          this.friendshipStatus = FriendshipStatus.Friends;
          this.fetchWorkouts();
        }
        }

      );
  }

  rejectFriendRequest() {
      this.friendsService.rejectFriendRequest(this.User.id).subscribe({
        next: () => this.friendshipStatus = FriendshipStatus.None
      });
  }

  unfriend() {
      this.friendsService.unfriend(this.User.id).subscribe({
        next: () => {
          this.friendshipStatus = FriendshipStatus.None;
          this.workouts = null;
          this.activityHidden = true;
        }
      })
  }

  getFriendshipStatus(){
    this.profileRequests.add(this.friendsService.getFriendshipStatus(this.User.id).subscribe(status => {
        this.friendshipStatus = status;
      }
    ));
  }

  getCurrentStreak(): number {
    if (!this.workouts || this.workouts.length === 0) return 0;

    const sortedWorkouts = [...this.workouts].sort((a, b) =>
      new Date(b.date).getTime() - new Date(a.date).getTime()
    );

    let streak = 0;
    const currentDate = new Date();
    currentDate.setHours(0, 0, 0, 0);

    for (const workout of sortedWorkouts) {
      const workoutDate = new Date(workout.date);
      workoutDate.setHours(0, 0, 0, 0);

      const daysDiff = Math.floor((currentDate.getTime() - workoutDate.getTime()) / (1000 * 60 * 60 * 24));

      if (daysDiff === streak) {
        streak++;
      } else if (daysDiff > streak) {
        break;
      }
    }

    return streak;
  }

  getMonthlyData() {
    const months = [
      'January', 'February', 'March', 'April', 'May', 'June',
      'July', 'August', 'September', 'October', 'November', 'December'
    ];

    const maxCount = this.maxWorkoutsAMonth();

    return months.map((name, index) => {
      const count = this.workoutsPerMonth(index);
      return {
        name,
        count,
        percentage: maxCount > 0 ? (count / maxCount) * 100 : 0
      };
    }).filter(month => month.count > 0 || month.name === months[new Date().getMonth()]);
  }

  protected readonly FriendshipStatus = FriendshipStatus;


}
