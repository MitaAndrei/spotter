import {Food} from "./food.model";

export interface LoggedFood{
  id: string;
  food: Food;
  date: Date;
  grams: number;
}
